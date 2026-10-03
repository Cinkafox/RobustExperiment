/**
 * Деформация модели по позе скелета (linear blend skinning).
 *
 * Формат костей в инструменте соответствует файлам движка, где и position,
 * и rotation заданы в мировых координатах: bind-матрица кости — это просто
 *
 *   bind(bone) = T(position) · R(rotation)
 *
 * без офсета от родителя и без композиции родительского поворота.
 * Признак этого видно на реальном скелете: у всех костей правой стороны
 * rotation = 0,180,0, и если бы это были локальные углы, сторона
 * накопила бы 180/360/540.
 *
 * Иерархия при этом не выбрасывается — она отвечает за то, что поворот
 * родителя уезжает вместе с детьми. Поэтому поворот раскладывается на
 * цепочку мировых деформаций, каждая вокруг своего сустава:
 *
 *   skin(root)    = T(p) · R(d) · T(-p)
 *   skin(bone)    = skin(parent) · T(p) · R(d) · T(-p)
 *
 * где p — bind-позиция кости, d — пробный поворот относительно
 * bone.rotation (нулевой, если кость не повёрнута).
 * Вершина смешивается по весам:
 *
 *   v' = Σ weight · skin(bone) · v
 *
 * Без пробной позы все skin = identity, поэтому bind-матрица и поведение
 * инструмента не меняются: якоря стоят ровно в bone.position.
 *
 * Пробная поза (state.pose с поворотами и state.poseOffset со сдвигами
 * сустава) не трогает данные кости и не попадает в экспорт, пока
 * пользователь не нажмёт «Записать в кость» — тогда значения переносятся
 * в bone.rotation и bone.position.
 */
(function (SkinTool) {
  'use strict';

  /** Предохранитель от зацикливания при ошибочном родителе в дереве. */
  const MAX_TRAVERSAL_STEPS = 10000;

  /** @type {Map<number, THREE.Matrix4>} T(position) · R(rotation) */
  let bindMatrices = new Map();

  /** @type {Map<number, THREE.Matrix4>} цепочка мировых деформаций */
  let skinMatrices = new Map();

  /** @type {Map<number, THREE.Matrix4>} skin · bind — мировая поза кости */
  let poseMatrices = new Map();

  /** @type {Array<{boneId:number, weight:number}[]|null>} привязка вершины к костям */
  let vertexBindings = [];

  let bindCounts = 0;
  let posedCounts = 0;

  /** Буфер результата переиспользуется — слайдер дёргается десятками кадров. */
  let posedPositions = new Float32Array(0);

  /** @param {number} boneId */
  function isPosed(boneId) {
    return SkinTool.model.state.pose[boneId] !== undefined;
  }

  /**
   * Пересчитывает всё, что зависит от состава скелета:
   * привязку вершин, bind-матрицы и текущую позу.
   * Вызывается после импорта YAML, создания/удаления костей
   * и изменения индексов или весов.
   */
  function rebuild() {
    const { state, getWeight } = SkinTool.model;

    vertexBindings = new Array(state.vertices.length).fill(null);

    state.bones.forEach((bone) => {
      bone.indices.forEach((vertexIndex, slot) => {
        if (vertexIndex < 0 || vertexIndex >= state.vertices.length) return;

        if (!vertexBindings[vertexIndex]) vertexBindings[vertexIndex] = [];
        vertexBindings[vertexIndex].push({ boneId: bone.id, weight: getWeight(bone, slot) });
      });
    });

    bindMatrices = buildBindMatrices();
    update();
  }

  /**
   * Bind-матрицы: мировая позиция и абсолютный поворот кости.
   * Порядок обхода здесь не важен — иерархия не участвует.
   * @returns {Map<number, THREE.Matrix4>}
   */
  function buildBindMatrices() {
    const THREE = SkinTool.THREE;
    const { state } = SkinTool.model;

    const matrices = new Map();
    const quaternion = new THREE.Quaternion();
    const euler = new THREE.Euler();
    const position = new THREE.Vector3();
    const unitScale = new THREE.Vector3(1, 1, 1);

    state.bones.forEach((bone) => {
      euler.set(
        THREE.MathUtils.degToRad(bone.rotation[0]),
        THREE.MathUtils.degToRad(bone.rotation[1]),
        THREE.MathUtils.degToRad(bone.rotation[2]),
        SkinTool.EULER_ORDER
      );

      position.set(bone.position[0], bone.position[1], bone.position[2]);

      matrices.set(bone.id, new THREE.Matrix4().compose(
        position,
        quaternion.setFromEuler(euler),
        unitScale
      ));
    });

    return matrices;
  }

  /**
   * Цепочка деформаций: поворот родителя уезжает вместе с детьми.
   * @returns {Map<number, THREE.Matrix4>}
   */
  function buildSkinMatrices() {
    const THREE = SkinTool.THREE;
    const { state, getChildBones, getBone } = SkinTool.model;

    const matrices = new Map();
    const visited = new Set();
    const quaternion = new THREE.Quaternion();
    const euler = new THREE.Euler();
    const pivot = new THREE.Vector3();
    const position = new THREE.Vector3();

    function walk(bone, parentSkin) {
      if (!bone || visited.has(bone.id)) return;
      visited.add(bone.id);

      pivot.set(bone.position[0], bone.position[1], bone.position[2]);
      position.copy(pivot);

      // Пробный поворот относительно bone.rotation: у неповёрнутой кости он нулевой.
      const rotation = isPosed(bone.id) ? state.pose[bone.id] : [0, 0, 0];
      euler.set(
        THREE.MathUtils.degToRad(rotation[0]),
        THREE.MathUtils.degToRad(rotation[1]),
        THREE.MathUtils.degToRad(rotation[2]),
        SkinTool.EULER_ORDER
      );
      quaternion.setFromEuler(euler);

      // Смещение сустава (state.poseOffset) двигает кость целиком вместе с
      // потомками: анимации хранят в кадрах мировую позицию, а деформатору
      // нужен сдвиг относительно bind-координаты.
      if (state.poseOffset[bone.id]) {
        const offset = state.poseOffset[bone.id];
        position.x += offset[0];
        position.y += offset[1];
        position.z += offset[2];
      }

      // Поворот вокруг собственного сустава кости: T(p + offset) · R · T(-p).
      // Обратный перенос остаётся по bind-суставу: если сдвинуть и его, то
      // у неповёрнутой кости T(p + offset) · T(-p - offset) схлопнётся в I и
      // смещение исчезнет.
      const own = new THREE.Matrix4()
        .makeTranslation(position.x, position.y, position.z)
        .multiply(new THREE.Matrix4().makeRotationFromQuaternion(quaternion));

      own.multiply(new THREE.Matrix4().makeTranslation(-pivot.x, -pivot.y, -pivot.z));

      // Родительская деформация применяется первой, поэтому её матрица
      // идёт слева. Каждый шаг создаёт свои объекты — иначе матрица
      // родителя переписывается в рекурсии.
      const skin = parentSkin ? parentSkin.clone().multiply(own) : own;

      matrices.set(bone.id, skin);

      if (visited.size < MAX_TRAVERSAL_STEPS) {
        getChildBones(bone.id).forEach((child) => walk(child, skin));
      }
    }

    getChildBones(null).forEach((root) => walk(root, null));

    // Кости вне дерева (цикл, потерянный родитель) не должны остаться
    // в bind-позе навсегда — считаем их кореньми.
    state.bones.forEach((bone) => {
      if (!matrices.has(bone.id)) walk(bone, null);
    });

    return matrices;
  }

  /** Пересчитывает позу и двигает вершины. */
  function update() {
    skinMatrices = buildSkinMatrices();
    poseMatrices = new Map();

    skinMatrices.forEach((skin, boneId) => {
      const bind = bindMatrices.get(boneId);
      if (bind) poseMatrices.set(boneId, skin.clone().multiply(bind));
    });

    SkinTool.scene.updatePositions(deformVertices());
    posedCounts = new Set([
      ...Object.keys(SkinTool.model.state.pose),
      ...Object.keys(SkinTool.model.state.poseOffset)
    ]).size;
  }

  /**
   * Смешивает вершины по весам их костей.
   * @returns {Float32Array} координаты вершин после деформации
   */
  function deformVertices() {
    const { state } = SkinTool.model;
    const source = state.vertices;
    const normalize = state.normalizeWeights;
    const point = new SkinTool.THREE.Vector3();

    if (posedPositions.length !== source.length * 3) {
      posedPositions = new Float32Array(source.length * 3);
    }

    let boundCount = 0;

    for (let i = 0; i < source.length; i++) {
      const vertex = source[i];
      const bindings = vertexBindings[i];

      if (!bindings || bindings.length === 0) {
        posedPositions[i * 3] = vertex.x;
        posedPositions[i * 3 + 1] = vertex.y;
        posedPositions[i * 3 + 2] = vertex.z;
        continue;
      }

      boundCount++;

      let sumX = 0;
      let sumY = 0;
      let sumZ = 0;
      let weightSum = 0;

      for (let k = 0; k < bindings.length; k++) {
        const { boneId, weight } = bindings[k];
        const skin = skinMatrices.get(boneId);
        if (!skin || weight === 0) continue;

        point.set(vertex.x, vertex.y, vertex.z).applyMatrix4(skin);

        sumX += point.x * weight;
        sumY += point.y * weight;
        sumZ += point.z * weight;
        weightSum += weight;
      }

      // Веса из YAML могут быть не нормализованы (например, вершина
      // закреплена за двумя костями с весом 1) — тогда делим на сумму,
      // иначе вершина улетела бы от скелета.
      const scale = normalize && weightSum > 0 ? 1 / weightSum : 1;

      posedPositions[i * 3] = sumX * scale;
      posedPositions[i * 3 + 1] = sumY * scale;
      posedPositions[i * 3 + 2] = sumZ * scale;
    }

    bindCounts = boundCount;
    return posedPositions;
  }

  // ---------- Пробная поза ----------

  /** Ставит пробный поворот кости и пересчитывает деформацию. */
  function setPoseRotation(boneId, rotation) {
    SkinTool.model.state.pose[boneId] = [rotation[0], rotation[1], rotation[2]];
    update();
  }

  /**
   * Пробный поворот кости — дельта к bind, а не мировой угол.
   *
   * Именно дельту накладывает деформатор, и именно её показывают кольца
   * и ползунки. Раньше здесь возвращался bone.rotation, из-за чего первое
   * же движение ползунка у зеркальной кости (bind 0,180,0) доворачивало
   * её ещё на 180°.
   */
  function getPoseRotation(boneId) {
    return SkinTool.model.state.pose[boneId] || [0, 0, 0];
  }

  /**
   * Сдвигает сустав кости в пробной позе — [dx, dy, dz] относительно
   * bind-координаты. Используется анимациями: их кадры хранят позицию
   * в мире, а деформатору нужен именно сдвиг.
   */
  function setPoseOffset(boneId, offset) {
    SkinTool.model.state.poseOffset[boneId] = [offset[0], offset[1], offset[2]];
    update();
  }

  /** Заменяет всю пробную позу разом — так дешевле, чем ставить по одной кости. */
  function setPose(pose, offsets) {
    const { state } = SkinTool.model;

    state.pose = pose || {};
    state.poseOffset = offsets || {};

    update();
  }

  /**
   * Переносит пробную позу в данные костей — попадёт в экспорт.
   *
   * Пересобираем всё (rebuild, а не update), потому что bind-матрицы
   * строятся из bone.position и bone.rotation: после записи они уже
   * неверны, и без пересборки модель вернулась бы к старой позе,
   * хотя данные уже изменены.
   */
  function applyPose() {
    const { state } = SkinTool.model;

    Object.keys(state.pose).forEach((boneId) => {
      const bone = SkinTool.model.getBone(Number(boneId));
      // Пробная поза — дельта к bind, а bone.rotation хранит мировой угол.
      // Без пересчёта зеркальная кость (bind 0,180,0) теряла бы разворот.
      if (bone) {
        bone.rotation = SkinTool.animation.rotationAbsolute(bone.rotation, state.pose[boneId]);
      }
    });

    Object.keys(state.poseOffset).forEach((boneId) => {
      const bone = SkinTool.model.getBone(Number(boneId));
      const offset = state.poseOffset[boneId];
      if (bone) bone.position = [
        bone.position[0] + offset[0],
        bone.position[1] + offset[1],
        bone.position[2] + offset[2]
      ];
    });

    state.pose = {};
    state.poseOffset = {};
    rebuild();
  }

  /** Сбрасывает пробную позу: @param {number} boneId — null сбрасывает всё */
  function discardPose(boneId) {
    const { state } = SkinTool.model;

    if (boneId === null || boneId === undefined) {
      state.pose = {};
      state.poseOffset = {};
    } else {
      delete state.pose[boneId];
      delete state.poseOffset[boneId];
    }

    update();
  }

  /**
   * Мировая матрица кости в текущей позе — её используют якоря.
   * Без пробной позы совпадает с bind-матрицей, то есть с bone.position.
   * @param {number} boneId
   * @returns {THREE.Matrix4|null}
   */
  function getBoneMatrix(boneId) {
    return poseMatrices.get(boneId) || bindMatrices.get(boneId) || null;
  }

  /** Информация о влиянии кости — для панели теста. */
  function getBoneInfluence(boneId) {
    const { state, getWeight } = SkinTool.model;
    const bone = SkinTool.model.getBone(boneId);

    if (!bone) return { count: 0, weightSum: 0, shared: 0, vertexCount: state.vertices.length };

    let weightSum = 0;
    let shared = 0;

    bone.indices.forEach((vertexIndex, slot) => {
      weightSum += getWeight(bone, slot);
      const bindings = vertexBindings[vertexIndex];
      if (bindings && bindings.length > 1) shared++;
    });

    return {
      count: bone.indices.length,
      weightSum,
      shared,
      vertexCount: state.vertices.length
    };
  }

  SkinTool.deformer = {
    rebuild,
    update,
    setPoseRotation,
    getPoseRotation,
    setPoseOffset,
    setPose,
    applyPose,
    discardPose,
    getBoneMatrix,
    isPosed,
    getBoneInfluence,
    getStats: () => ({ bound: bindCounts, posed: posedCounts })
  };
})(window.SkinTool);
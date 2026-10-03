/**
 * Оверлей режима позинга: скелет и окружности вращения кости.
 *
 * Скелет рисуется рёбрами между суставами в цвете костей: кость в пробной
 * позе светлеет, выбранная становится белой. Рёбра идут по текущей позе,
 * поэтому скелет двигается вместе с мешем.
 *
 * Окружности рисуются только для выбранной кости — три кольца в плоскостях
 * её локальных осей (X — красное, Y — зелёное, Z — синее; те же цвета, что
 * у ползунков позинга) плюс дуга от bind-направления до текущего. За её
 * концами стоят засечки: короткая — где ось была в bind-посе, длинная —
 * где она сейчас. Так видно и величину, и направление поворота.
 *
 * Размер гизмо задан в пикселях, а не в мировых единицах: иначе при
 * приближении камеры кольца разрастались бы на весь экран, а при отдалении
 * превращались в точку. Радиус пересчитывается каждый кадр из расстояния
 * до сустава, поэтому при любом положении камеры кольцо занимает ровно
 * RADIUS_PX. Кольца при этом остаются настоящими окружностями в 3D:
 * ближняя к камере половина крупнее дальней, как и в любом редакторе.
 *
 * По кольцу можно повернуть кость: зажатая левая кнопка тянет пробный
 * поворот вокруг соответствующей оси. Угол берётся из экранных координат
 * курсора — тем же параметром, которым нарисована дуга, поэтому кольцо
 * следует за мышью без расхождений (см. pickRing/updateRingDrag).
 *
 * Модуль ничего не знает про интерфейс и ни о ком не подписан: он читает
 * состояние (state.currentTab, state.pose, state.selectedBoneId) и
 * перерисовывается каждый кадр, поэтому новая точка изменения позы
 * не сможет его забыть.
 */
(function (SkinTool) {
  'use strict';

  /** Точек в окружности (замыкается повтором первой). */
  const RING_SEGMENTS = 72;

  /** Радиус колец на экране: одинаков при любом положении камеры. */
  const RADIUS_PX = 80;

  /** Захват кольца мышью, пиксели от ближайшей точки окружности. */
  const RING_PICK_PX = 14;

  /**
   * Минимум для малой полуоси кольца на экране, в пикселях.
   *
   * Кольцо, увиденное с ребра, сплющивается в отрезок, проходящий через
   * сустав: любая точка рядом с суставом оказывается «на» таком кольце,
   * а угол по нему восстановить нельзя. Поэтому захватывать и вращать
   * такие кольца бессмысленно — они выпадают из подбора.
   */
  const RING_MIN_MINOR_PX = 8;

  /** Плоскость кольца: нормаль — ось вращения, tangent — ось в этой плоскости. */
  const AXIS_PLANES = [
    { normal: 0, tangent: 1 }, // X: касательные Y и Z
    { normal: 1, tangent: 2 }, // Y: касательные Z и X
    { normal: 2, tangent: 0 }  // Z: касательные X и Y
  ];

  /** Подписи осей для сообщений в строке статуса. */
  const AXIS_LABELS = ['X', 'Y', 'Z'];

  /** Единичные векторы локальных осей — индекс совпадает с осью. */
  const UNIT_AXES = [];

  const WIRE_OPACITY = 0.9;
  const RING_OPACITY = 0.8;
  const BIND_AXIS_OPACITY = 0.35;
  const TICK_INNER = 0.82;

  /** Порядок отрисовки: скелет под гизмо, но всё это поверх меша. */
  const RENDER_ORDER_WIRE = 5;
  const RENDER_ORDER_GIZMO = 20;
  const WIRE_NAME = 'skeletonWire';
  const GIZMO_NAME = 'skeletonGizmo';

  /** Вырожденная проекция — ось повёрнута ровно на 90° к касательной. */
  const EPSILON = 1e-6;

  /** Индексы осей — 0..2, а -1 означает «нет оси». */
  const NO_AXIS = -1;

  /** Рёбра скелета: одна линия на весь риг. */
  let wire = null;
  let wireSegmentCount = -1;

  let gizmoGroup = null;
  const rings = [];
  const arcs = [];
  const ticks = [];
  let pointers = null;
  let posedAxes = null;
  let bindAxes = null;

  /** Касательные оси каждого кольца в мире — нужны для поиска и вращения. */
  const ringTangents = [];
  const ringBitangents = [];
  const ringVisible = [];

  /** Ось, которую сейчас тянут мышью, или NO_AXIS. */
  let draggingAxis = NO_AXIS;

  // Рабочие объекты: покадровая отрисовка не должна ничего создавать.
  let pivot = null;
  let radius = 1;
  const axisScratch = [];
  let eulerScratch = null;
  let poseQuaternion = null;
  let bindQuaternion = null;
  let scaleScratch = null;
  let normalScratch = null;
  let tangentScratch = null;
  let currentTangentScratch = null;
  let bitangentScratch = null;
  let crossScratch = null;
  let jointScratch = null;
  let projectScratch = null;
  let closestScratch = null;
  let depthScratch = null;

  /** Угол кольца в момент захвата: от него считаем относительную дельту. */
  let dragStartAngle = 0;

  function init() {
    const THREE = SkinTool.THREE;

    // Three.js доступен только после загрузки — создаём объекты здесь,
    // а не на верхнем уровне модуля.
    UNIT_AXES.push(new THREE.Vector3(1, 0, 0));
    UNIT_AXES.push(new THREE.Vector3(0, 1, 0));
    UNIT_AXES.push(new THREE.Vector3(0, 0, 1));

    pivot = new THREE.Vector3();
    eulerScratch = new THREE.Euler();
    poseQuaternion = new THREE.Quaternion();
    bindQuaternion = new THREE.Quaternion();
    scaleScratch = new THREE.Vector3(1, 1, 1);
    normalScratch = new THREE.Vector3();
    tangentScratch = new THREE.Vector3();
    currentTangentScratch = new THREE.Vector3();
    bitangentScratch = new THREE.Vector3();
    crossScratch = new THREE.Vector3();
    jointScratch = new THREE.Vector3();
    projectScratch = new THREE.Vector3();
    depthScratch = new THREE.Vector3();
    closestScratch = { distance: 0, angle: 0 };

    for (let i = 0; i < 3; i++) {
      axisScratch.push(new THREE.Vector3());
      ringTangents.push(new THREE.Vector3());
      ringBitangents.push(new THREE.Vector3());
      ringVisible.push(false);
    }

    createWire();
    createGizmo();

    SkinTool.scene.addFrameListener(update);
  }

  function bind() {
    syncToggleButton();

    SkinTool.dom.$('btnToggleSkeleton').addEventListener('click', function () {
      SkinTool.model.state.showSkeleton = !SkinTool.model.state.showSkeleton;
      syncToggleButton();
    });
  }

  // ---------- Скелет ----------

  function createWire() {
    const THREE = SkinTool.THREE;

    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.BufferAttribute(new Float32Array(0), 3));
    geometry.setAttribute('color', new THREE.BufferAttribute(new Float32Array(0), 3));

    wire = new THREE.LineSegments(geometry, new THREE.LineBasicMaterial({
      vertexColors: true,
      transparent: true,
      opacity: WIRE_OPACITY,
      depthTest: false
    }));
    wire.renderOrder = RENDER_ORDER_WIRE;
    wire.frustumCulled = false;
    wire.visible = false;
    wire.name = WIRE_NAME;

    SkinTool.scene.add(wire);
  }

  /** Пересоздаёт буфер рёбер — только когда изменилось число костей с родителем. */
  function rebuildWire(segmentCount) {
    const vertices = segmentCount * 2;
    wire.geometry.setAttribute('position', new THREE.BufferAttribute(new Float32Array(vertices * 3), 3));
    wire.geometry.setAttribute('color', new THREE.BufferAttribute(new Float32Array(vertices * 3), 3));
    wireSegmentCount = segmentCount;
  }

  function updateWire() {
    const { state, BONE_COLORS, getBone } = SkinTool.model;

    let segmentCount = 0;
    state.bones.forEach(function (bone) {
      if (bone.parentId !== null && bone.parentId !== undefined) segmentCount++;
    });

    if (segmentCount !== wireSegmentCount) rebuildWire(segmentCount);
    if (segmentCount === 0) {
      wire.visible = false;
      return;
    }
    wire.visible = true;

    const position = wire.geometry.attributes.position.array;
    const color = wire.geometry.attributes.color.array;
    let vertex = 0;

    state.bones.forEach(function (bone) {
      const parent = getBone(bone.parentId);
      if (!parent) return;

      writeJoint(bone, position, vertex);
      writeJoint(parent, position, vertex + 1);

      // Выбранная кость белая, пробная поза — светлее остальных.
      // Ребро красится цветом кости-ребёнка, поэтому обе его вершины
      // получают один и тот же цвет — иначе край оставался бы чёрным.
      const isSelected = bone.id === state.selectedBoneId;
      const isPosed = SkinTool.deformer.isPosed(bone.id);
      const boneColor = BONE_COLORS[bone.id % BONE_COLORS.length];
      const mix = isSelected ? 1 : isPosed ? 0.8 : 0.45;
      writeBoneColor(color, vertex * 3, boneColor, mix);
      writeBoneColor(color, vertex * 3 + 3, boneColor, mix);

      vertex += 2;
    });

    wire.geometry.attributes.position.needsUpdate = true;
    wire.geometry.attributes.color.needsUpdate = true;
  }

  /**
   * Мировая позиция сустава: из матрицы позы, а без неё — из bone.position.
   * @param {object} bone
   * @param {Float32Array} target
   * @param {number} vertexIndex
   */
  function writeJoint(bone, target, vertexIndex) {
    const matrix = SkinTool.deformer.getBoneMatrix(bone.id);

    if (matrix) {
      jointScratch.setFromMatrixPosition(matrix);
    } else {
      jointScratch.set(bone.position[0], bone.position[1], bone.position[2]);
    }

    target[vertexIndex * 3] = jointScratch.x;
    target[vertexIndex * 3 + 1] = jointScratch.y;
    target[vertexIndex * 3 + 2] = jointScratch.z;
  }

  /**
   * @param {Float32Array} target
   * @param {number} offset
   * @param {number} hex — цвет кости
   * @param {number} mix — насколько подмешать белый (0 — как есть, 1 — белый)
   */
  function writeBoneColor(target, offset, hex, mix) {
    target[offset] = mixWhite((hex >> 16) & 255, mix);
    target[offset + 1] = mixWhite((hex >> 8) & 255, mix);
    target[offset + 2] = mixWhite(hex & 255, mix);
  }

  function mixWhite(channel, mix) {
    return (channel / 255) * (1 - mix) + mix;
  }

  // ---------- Окружности вращения ----------

  function createGizmo() {
    const THREE = SkinTool.THREE;
    const axisColors = SkinTool.model.AXIS_COLORS;

    gizmoGroup = new THREE.Group();
    gizmoGroup.visible = false;
    gizmoGroup.renderOrder = RENDER_ORDER_GIZMO;
    gizmoGroup.name = GIZMO_NAME;
    SkinTool.scene.add(gizmoGroup);

    for (let axisIndex = 0; axisIndex < 3; axisIndex++) {
      const color = axisColors[axisIndex];

      const ring = createLoop(color, RING_OPACITY);
      rings.push(ring);

      const arc = createLoop(color, 1);
      arcs.push(arc);

      const tick = createSegments(0xffffff, 0.45);
      ticks.push(tick);

      gizmoGroup.add(ring, arc, tick);
    }

    // Указатели: от центра кольца вдоль текущих касательных осей.
    pointers = createSegmentsWithVertexColors(axisColors, 0.9);
    gizmoGroup.add(pointers);

    // Крестик осей выбранной кости: цветной — текущая поза, серый — bind.
    posedAxes = createSegmentsWithVertexColors(axisColors, 0.9);
    bindAxes = createSegments(0xcccccc, BIND_AXIS_OPACITY);
    gizmoGroup.add(posedAxes, bindAxes);
  }

  /** Кольцо или дуга: одна ломаная на RING_SEGMENTS + 1 точек. */
  function createLoop(color, opacity) {
    const THREE = SkinTool.THREE;
    const vertexCount = RING_SEGMENTS + 1;

    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.BufferAttribute(new Float32Array(vertexCount * 3), 3));

    const line = new THREE.Line(geometry, new THREE.LineBasicMaterial({
      color: color,
      transparent: true,
      opacity: opacity,
      depthTest: false
    }));

    line.renderOrder = RENDER_ORDER_GIZMO;
    line.frustumCulled = false;
    line.visible = false;

    return line;
  }

  /** Отрезок из двух точек. */
  function createSegments(color, opacity) {
    const THREE = SkinTool.THREE;

    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.BufferAttribute(new Float32Array(6), 3));

    const line = new THREE.LineSegments(geometry, new THREE.LineBasicMaterial({
      color: color,
      transparent: true,
      opacity: opacity,
      depthTest: false
    }));

    line.renderOrder = RENDER_ORDER_GIZMO;
    line.frustumCulled = false;
    line.visible = false;

    return line;
  }

  /** Отрезки, у которых каждый свой цвет (крестики осей). */
  function createSegmentsWithVertexColors(colors, opacity) {
    const THREE = SkinTool.THREE;

    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.BufferAttribute(new Float32Array(18), 3));
    geometry.setAttribute('color', new THREE.BufferAttribute(new Float32Array(18), 3));

    const line = new THREE.LineSegments(geometry, new THREE.LineBasicMaterial({
      vertexColors: true,
      transparent: true,
      opacity: opacity,
      depthTest: false
    }));

    line.renderOrder = RENDER_ORDER_GIZMO;
    line.frustumCulled = false;
    line.visible = false;

    const colorArray = geometry.attributes.color.array;
    for (let axisIndex = 0; axisIndex < 3; axisIndex++) {
      const hex = colors[axisIndex];
      writeBoneColor(colorArray, axisIndex * 6, hex, 0);
      writeBoneColor(colorArray, axisIndex * 6 + 3, hex, 0);
    }

    return line;
  }

  function updateGizmo() {
    const THREE = SkinTool.THREE;
    const bone = SkinTool.model.getSelectedBone();

    if (!bone) {
      gizmoGroup.visible = false;
      return;
    }
    gizmoGroup.visible = true;

    eulerScratch.set(
      THREE.MathUtils.degToRad(bone.rotation[0]),
      THREE.MathUtils.degToRad(bone.rotation[1]),
      THREE.MathUtils.degToRad(bone.rotation[2]),
      SkinTool.EULER_ORDER
    );
    bindQuaternion.setFromEuler(eulerScratch);

    // Текущая мировая ориентация кости — из матрицы позы деформера.
    const matrix = SkinTool.deformer.getBoneMatrix(bone.id);
    if (matrix) {
      matrix.decompose(pivot, poseQuaternion, scaleScratch);
    } else {
      pivot.set(bone.position[0], bone.position[1], bone.position[2]);
      poseQuaternion.copy(bindQuaternion);
    }

    radius = getRingRadius();

    writeAxes(posedAxes, poseQuaternion);
    bindAxes.visible = true;
    writeAxes(bindAxes, bindQuaternion);
    posedAxes.visible = true;

    AXIS_PLANES.forEach(function (plane, axisIndex) {
      updateAxisGizmo(axisIndex, plane);
    });
  }

  /**
   * Окружность одной оси: кольцо, дуга поворота, засечки и указатель.
   *
   * Дуга считается по касательной оси: её bind-направление — начало,
   * текущее — конец, а сама дуга лежит в плоскости кольца. Для поворота
   * по одной оси угол выходит точным, для комбинации осей — это проекция
   * на плоскость своего кольца, как и в гизмо настоящих редакторов.
   *
   * @param {number} axisIndex
   * @param {{normal:number, tangent:number}} plane
   */
  function updateAxisGizmo(axisIndex, plane) {
    const THREE = SkinTool.THREE;

    setAxisVector(normalScratch, plane.normal, poseQuaternion);
    setAxisVector(tangentScratch, plane.tangent, bindQuaternion);
    setAxisVector(currentTangentScratch, plane.tangent, poseQuaternion);

    // Касательные лежат в плоскости кольца по определению, но после
    // поворота кости могут из неё выйти — приводим к плоскости.
    tangentScratch.addScaledVector(normalScratch, -tangentScratch.dot(normalScratch));
    currentTangentScratch.addScaledVector(normalScratch, -currentTangentScratch.dot(normalScratch));

    if (tangentScratch.lengthSq() < EPSILON || currentTangentScratch.lengthSq() < EPSILON) {
      rings[axisIndex].visible = false;
      arcs[axisIndex].visible = false;
      ticks[axisIndex].visible = false;
      ringVisible[axisIndex] = false;
      return;
    }

    tangentScratch.normalize();
    currentTangentScratch.normalize();

    // Положительное направление — по правилу правой руки: касательная
    // уезжает в сторону бикаса.
    bitangentScratch.crossVectors(normalScratch, tangentScratch);

    fillLoop(rings[axisIndex], tangentScratch, bitangentScratch, 0, Math.PI * 2);
    rings[axisIndex].visible = true;

    const sweptAngle = Math.atan2(
      normalScratch.dot(crossScratch.crossVectors(tangentScratch, currentTangentScratch)),
      tangentScratch.dot(currentTangentScratch)
    );

    fillLoop(arcs[axisIndex], tangentScratch, bitangentScratch, 0, sweptAngle);
    arcs[axisIndex].visible = Math.abs(sweptAngle) > 0.5 * THREE.MathUtils.DEG2RAD;

    fillTick(ticks[axisIndex], tangentScratch);
    ticks[axisIndex].visible = true;

    // Базис кольца сохраняем отдельными объектами: рабочие векторы
    // перезаписываются следующей осью, а нужен он потом — при поиске
    // кольца мышью и при повороте кости.
    ringTangents[axisIndex].copy(tangentScratch);
    ringBitangents[axisIndex].copy(bitangentScratch);
    ringVisible[axisIndex] = true;

    writePointer(axisIndex, currentTangentScratch);
  }

  /** Короткая засечка на кольце — где ось была в bind-посе. */
  function fillTick(line, u) {
    const array = line.geometry.attributes.position.array;

    array[0] = pivot.x + u.x * radius * TICK_INNER;
    array[1] = pivot.y + u.y * radius * TICK_INNER;
    array[2] = pivot.z + u.z * radius * TICK_INNER;
    array[3] = pivot.x + u.x * radius;
    array[4] = pivot.y + u.y * radius;
    array[5] = pivot.z + u.z * radius;

    line.geometry.attributes.position.needsUpdate = true;
  }

  /** Указатель от центра кольца вдоль текущей касательной оси. */
  function writePointer(axisIndex, direction) {
    const array = pointers.geometry.attributes.position.array;
    const offset = axisIndex * 6;

    array[offset] = pivot.x;
    array[offset + 1] = pivot.y;
    array[offset + 2] = pivot.z;
    array[offset + 3] = pivot.x + direction.x * radius;
    array[offset + 4] = pivot.y + direction.y * radius;
    array[offset + 5] = pivot.z + direction.z * radius;

    pointers.geometry.attributes.position.needsUpdate = true;
    pointers.visible = true;
  }

  /** Крестик осей кости: от сустава до конца кольца по всем трём осям. */
  function writeAxes(line, quaternion) {
    const array = line.geometry.attributes.position.array;

    for (let axisIndex = 0; axisIndex < 3; axisIndex++) {
      setAxisVector(axisScratch[axisIndex], axisIndex, quaternion);
      const offset = axisIndex * 6;

      array[offset] = pivot.x;
      array[offset + 1] = pivot.y;
      array[offset + 2] = pivot.z;
      array[offset + 3] = pivot.x + axisScratch[axisIndex].x * radius;
      array[offset + 4] = pivot.y + axisScratch[axisIndex].y * radius;
      array[offset + 5] = pivot.z + axisScratch[axisIndex].z * radius;
    }

    line.geometry.attributes.position.needsUpdate = true;
  }

  /**
   * Дуга (или полное кольцо) в плоскости базиса (u, v).
   * @param {THREE.Line} line
   * @param {THREE.Vector3} u
   * @param {THREE.Vector3} v
   * @param {number} from — начало дуги в радианах
   * @param {number} to — конец дуги в радианах
   */
  function fillLoop(line, u, v, from, to) {
    const array = line.geometry.attributes.position.array;

    for (let i = 0; i <= RING_SEGMENTS; i++) {
      const angle = from + (to - from) * (i / RING_SEGMENTS);
      const cos = Math.cos(angle) * radius;
      const sin = Math.sin(angle) * radius;

      array[i * 3] = pivot.x + u.x * cos + v.x * sin;
      array[i * 3 + 1] = pivot.y + u.y * cos + v.y * sin;
      array[i * 3 + 2] = pivot.z + u.z * cos + v.z * sin;
    }

    line.geometry.attributes.position.needsUpdate = true;
  }

  /**
   * Мировая позиция локальной оси кости в заданной ориентации.
   * @param {THREE.Vector3} target
   * @param {number} axisIndex
   * @param {THREE.Quaternion} quaternion
   */
  function setAxisVector(target, axisIndex, quaternion) {
    const unit = UNIT_AXES[axisIndex];
    target.copy(unit).applyQuaternion(quaternion);
  }

  /**
   * Радиус колец в мировых единицах, дающий RADIUS_PX на экране.
   *
   * У перспективной камеры на глубине d высота видимой части кадра равна
   * 2·d·tan(fov/2), поэтому искомый мировой радиус равен
   * RADIUS_PX · 2·d·tan(fov/2) / height.
   *
   * Глубиной считаем проекцию вектора «камера → сустав» на направление
   * взгляда, а не евклидово расстояние до сустава: размер на экране
   * зависит именно от глубины, поэтому при взгляде сбоку кольцо не
   * «раздувается» вместе с расстоянием. Считается каждый кадр — при
   * движении камеры кольцо остаётся на экране того же размера.
   */
  function getRingRadius() {
    const THREE = SkinTool.THREE;
    const camera = SkinTool.scene.getCamera();
    const height = SkinTool.scene.getViewport().clientHeight;

    if (!height || !camera) return 1;

    camera.getWorldDirection(depthScratch);
    const depth = projectScratch.copy(pivot).sub(camera.position).dot(depthScratch);
    const halfHeightAtDepth = depth * Math.tan(THREE.MathUtils.degToRad(camera.fov) / 2);

    if (!(halfHeightAtDepth > EPSILON)) return 1;

    return (RADIUS_PX * 2 * halfHeightAtDepth) / height;
  }

  // ---------- Вращение кости кольцом ----------

  /**
   * Кольцо в экранных координатах: центр, два вектора полуосей и их длины.
   * Вырожденный случай — сустав за камерой — отдаёт null.
   *
   * @param {number} axisIndex
   * @returns {{cx:number, cy:number, ux:number, uy:number, vx:number, vy:number,
   *            major:number, minor:number}|null}
   */
  function ringFrame(axisIndex) {
    const camera = SkinTool.scene.getCamera();
    const rect = SkinTool.scene.getViewport().getBoundingClientRect();

    const center = projectToScreen(null, camera, rect);
    if (center === null) return null;

    const tangent = projectToScreen(ringTangents[axisIndex], camera, rect);
    const bitangent = projectToScreen(ringBitangents[axisIndex], camera, rect);
    if (tangent === null || bitangent === null) return null;

    const ux = tangent.x - center.x;
    const uy = tangent.y - center.y;
    const vx = bitangent.x - center.x;
    const vy = bitangent.y - center.y;

    // Базис переносит единичную окружность в эллипс: большая полуось —
    // длина первого вектора, малая — площадь делённая на неё.
    const major = Math.hypot(ux, uy);
    const minor = major > EPSILON ? Math.abs(ux * vy - uy * vx) / major : 0;

    return { cx: center.x, cy: center.y, ux, uy, vx, vy, major, minor };
  }

  /**
   * Экранные координаты точки: сам сустав либо конец радиус-вектора.
   * @param {THREE.Vector3|null} direction
   * @returns {{x:number, y:number}|null} null, если точка за камерой
   */
  function projectToScreen(direction, camera, rect) {
    if (direction === null) {
      projectScratch.copy(pivot);
    } else {
      projectScratch.copy(pivot).addScaledVector(direction, radius);
    }

    projectScratch.project(camera);
    if (projectScratch.z >= 1) return null;

    return {
      x: rect.left + (projectScratch.x * 0.5 + 0.5) * rect.width,
      y: rect.top + (-projectScratch.y * 0.5 + 0.5) * rect.height
    };
  }

  /**
   * Ближайшая точка кольца до курсора: расстояние и параметр угла.
   * Угол нужен, чтобы при захвате не было скачка — начать нужно ровно
   * с того места, где взяли кольцо, а не с нуля.
   *
   * @param {{cx:number, cy:number, ux:number, uy:number, vx:number, vy:number}} frame
   * @returns {{distance:number, angle:number}} результат переиспользуется
   */
  function closestOnRing(frame, clientX, clientY) {
    let bestDistance = Infinity;
    let bestAngle = 0;

    let previousX = frame.cx + frame.ux;
    let previousY = frame.cy + frame.uy;

    for (let i = 1; i <= RING_SEGMENTS; i++) {
      const angle = (i / RING_SEGMENTS) * Math.PI * 2;
      const cos = Math.cos(angle);
      const sin = Math.sin(angle);
      const pointX = frame.cx + frame.ux * cos + frame.vx * sin;
      const pointY = frame.cy + frame.uy * cos + frame.vy * sin;

      const distance = distanceToSegment(clientX, clientY, previousX, previousY, pointX, pointY);
      if (distance < bestDistance) {
        bestDistance = distance;
        bestAngle = angle;
      }

      previousX = pointX;
      previousY = pointY;
    }

    closestScratch.distance = bestDistance;
    closestScratch.angle = bestAngle;
    return closestScratch;
  }

  /** Расстояние от точки до отрезка. */
  function distanceToSegment(px, py, ax, ay, bx, by) {
    const dx = bx - ax;
    const dy = by - ay;
    const lengthSq = dx * dx + dy * dy;

    let t = 0;
    if (lengthSq > EPSILON) t = Math.max(0, Math.min(1, ((px - ax) * dx + (py - ay) * dy) / lengthSq));

    const cx = ax + dx * t;
    const cy = ay + dy * t;

    return Math.hypot(px - cx, py - cy);
  }

  /**
   * Кольцо под курсором.
   *
   * Ищем в экранных координатах, а не лучом по сцене: луч с мировым
   * порогом цеплял бы меш и скелет, а главное — мировой порог не
   * соответствует нашей кольцевой геометрии. Кольца у трёх осей имеют
   * один и тот же центр и радиус, поэтому берём то, до которого курсор
   * ближе всего — при виде сбоку это как раз то кольцо, что развёрнуто
   * к камере.
   *
   * @param {number} clientX
   * @param {number} clientY
   * @returns {number} индекс оси или NO_AXIS
   */
  function pickRing(clientX, clientY) {
    if (!isVisible() || !SkinTool.model.getSelectedBone()) return NO_AXIS;

    let bestAxis = NO_AXIS;
    let bestDistance = RING_PICK_PX;

    for (let axisIndex = 0; axisIndex < 3; axisIndex++) {
      if (!ringVisible[axisIndex]) continue;

      const frame = ringFrame(axisIndex);
      if (frame === null) continue;

      // Кольцо с ребра не ловим: оно вырождается в отрезок через сустав
      // и перехватывало бы клики по самому суставу и по соседним кольцам.
      if (frame.minor < RING_MIN_MINOR_PX) continue;

      const closest = closestOnRing(frame, clientX, clientY);
      if (closest.distance < bestDistance) {
        bestDistance = closest.distance;
        bestAxis = axisIndex;
      }
    }

    return bestAxis;
  }

  /**
   * Угол вокруг кольца под курсором, в градусах.
   *
   * Точка на кольце — это (cos t, sin t), перенесённые базисом кольца
   * на экран, поэтому угол восстанавливается обратным преобразованием
   * того же базиса. Отсчёт от нуля совпадает с тем, по которому нарисована
   * дуга: ноль — на bind-направлении касательной.
   *
   * @param {number} clientX
   * @param {number} clientY
   * @returns {number|null} градусы или null, если вырождено
   */
  function ringAngleAt(clientX, clientY) {
    if (draggingAxis === NO_AXIS) return null;

    const frame = ringFrame(draggingAxis);
    if (frame === null || frame.minor < RING_MIN_MINOR_PX) return null;

    const determinant = frame.ux * frame.vy - frame.uy * frame.vx;
    if (Math.abs(determinant) < EPSILON) return null;

    const dx = clientX - frame.cx;
    const dy = clientY - frame.cy;

    const cos = (frame.vy * dx - frame.vx * dy) / determinant;
    const sin = (frame.ux * dy - frame.uy * dx) / determinant;

    return SkinTool.THREE.MathUtils.radToDeg(Math.atan2(sin, cos));
  }

  /**
   * Насколько курсор ушёл от места захвата, в градусах.
   *
   * Кольцо круглое, поэтому угол в точке захвата ничего не значит:
   * берём кольцо хоть за «спину» дуги — кость не должна прыгать.
   * Поэтому наружу отдаётся только поворот курсора относительно
   * точки захвата, а к позе его прибавляет вызывающий код.
   *
   * @returns {number|null} дельта в диапазоне (-180, 180]
   */
  function getRingDragDelta(clientX, clientY) {
    const angle = ringAngleAt(clientX, clientY);
    if (angle === null) return null;

    const delta = angle - dragStartAngle;
    return ((((delta + 180) % 360) + 360) % 360) - 180;
  }

  /**
   * Начать вращение кольцом.
   *
   * @param {number} axisIndex
   * @param {number} [clientX] точка захвата — если передана, запоминаем
   *   угол в ней как начало отсчёта
   */
  function beginRingDrag(axisIndex, clientX, clientY) {
    draggingAxis = axisIndex;

    const angle = clientX === undefined ? null : ringAngleAt(clientX, clientY);
    dragStartAngle = angle === null ? 0 : angle;
  }

  function endRingDrag() {
    draggingAxis = NO_AXIS;
    dragStartAngle = 0;
  }

  function getDraggingAxis() {
    return draggingAxis;
  }

  /** Подпись оси для подсказок: X, Y или Z. */
  function getAxisLabel(axisIndex) {
    return AXIS_LABELS[axisIndex] || '?';
  }

  // ---------- Покадровое обновление ----------

  /**
   * Оверлей нужен в позинге и в анимациях: на таймлайне видно, что
   * двигается, а кольца остаются только у выбранной кости.
   * В остальных вкладках он мешает.
   */
  function isVisible() {
    const { state } = SkinTool.model;
    const tabAllows = state.currentTab === 'pose' || state.currentTab === 'animation';

    return tabAllows && state.showSkeleton !== false;
  }

  function update() {
    if (!wire) return;

    if (!isVisible()) {
      wire.visible = false;
      gizmoGroup.visible = false;
      ringVisible[0] = false;
      ringVisible[1] = false;
      ringVisible[2] = false;
      return;
    }

    updateWire();
    updateGizmo();
  }

  function syncToggleButton() {
    const button = SkinTool.dom.$('btnToggleSkeleton');
    if (button) button.classList.toggle('active', SkinTool.model.state.showSkeleton);
  }

  SkinTool.skeleton = {
    init,
    bind,
    update,
    isVisible,
    pickRing,
    beginRingDrag,
    endRingDrag,
    getDraggingAxis,
    ringAngleAt,
    getRingDragDelta,
    getAxisLabel,
    getRadius: () => radius,
    getRadiusPx: () => RADIUS_PX,
    getMinMinorPx: () => RING_MIN_MINOR_PX
  };
})(window.SkinTool);
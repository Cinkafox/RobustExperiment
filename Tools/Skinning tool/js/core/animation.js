/**
 * Модель анимаций: клипы, дорожки и ключевые кадры.
 *
 * Формат повторяет то, что читает движок в прототипах `bodyAnimation`
 * (Content.Shared/Animations/Data): у клипа есть длина, флаг зацикливания
 * и дорожки; у дорожки — кость, тип свойства, режим интерполяции и
 * список кадров. Отличия от файла движка — только внутренние:
 *
 *   - время кадра здесь АБСОЛЮТНОЕ (секунды от начала клипа), потому что
 *     по нему двигается playhead и кадры можно ставить щелчком по линейке.
 *     В YAML движка keyTime — это ДЕЛЬТА от предыдущего кадра
 *     (см. AnimationTrackProperty.AdvancePlayback и файл
 *     Resources/Prototypes/Entities/Animations/alexandra.yml:
 *     0 / 0.25 / 0.25 / 0.25 / 0.25 при length 1s). Дельты считаются
 *     при экспорте — see toYaml.
 *
 *   - углы поворота хранятся в градусах, как bone.rotation и ползунки
 *     позинга; в YAML они уходят в радианах, как требует LocalAngleAsVector.
 *
 *   - дорожка знает кость по boneId, а не по имени: имена у пользователя
 *     меняются, а экспорт подставляет актуальное bone.name.
 */
(function (SkinTool) {
  'use strict';

  /** Режимы интерполяции — как AnimationInterpolationMode в движке. */
  const INTERPOLATION_MODES = ['Linear', 'Cubic', 'Nearest', 'Previous'];

  /**
   * Режим для новых дорожек, которые пользователь рисует в инструменте.
   *
   * Это авторское предпочтение инструмента, а не поведение движка: в
   * экспорт режим попадает явным полем, поэтому движок берёт именно его.
   */
  const DEFAULT_INTERPOLATION = 'Cubic';

  /**
   * Режим, который движок подставляет, если в файле поля нет.
   *
   * AnimationTrackProperty.InterpolationMode инициализируется как Linear,
   * и AnimationTrackProperty.AdvancePlayback смотрит именно на это
   * свойство. Если при импорте подставить свой DEFAULT_INTERPOLATION
   * (Cubic), превью между кадрами разойдётся с движком: на ключах
   * совпадёт, а между ними уедет на единицы градусов.
   */
  const ENGINE_DEFAULT_INTERPOLATION = 'Linear';

  const COMPONENT_TYPE = 'Content.Shared.Transform.Transform3dComponent';

  /**
   * Свойства кости, которые движок умеет анимировать.
   * kind — ключ в инструменте, property — имя свойства в YAML.
   * LocalAngleAsVector — углы Эйлера в радианах (pitch, yaw, roll),
   * LocalPosition — положение относительно родителя.
   */
  const PROPERTIES = {
    rotation: { property: 'LocalAngleAsVector', label: 'Поворот', unit: '°' },
    position: { property: 'LocalPosition', label: 'Позиция', unit: '' }
  };

  /** Кадры в одной точке времени считаются совпадающими. */
  const TIME_EPSILON = 1e-4;

  /** Градусы в радианы: в YAML движка углы в радианах, в инструменте — в градусах. */
  const DEG_TO_RAD = Math.PI / 180;

  let nextTrackId = 1;

  // ---------- Клипы ----------

  /**
   * @param {string} [id] — id прототипа в движке
   * @param {object} [options]
   * @returns {object} клип
   */
  function createClip(id, options) {
    const settings = options || {};

    return {
      id: id || nextClipId(),
      length: typeof settings.length === 'number' && settings.length > 0 ? settings.length : 1,
      looped: settings.looped !== false,
      tracks: []
    };
  }

  function nextClipId() {
    const { state } = SkinTool.model;
    let id = 'bodyAnimation';

    // id должен быть уникален внутри файла прототипов.
    while (state.animations.some((clip) => clip.id === id)) id += '2';

    return id;
  }

  /**
   * Новый пустой клип в общем списке.
   * @param {string} [id]
   * @returns {object}
   */
  function addClip(id) {
    const clip = createClip(id || nextClipId());

    getClips().push(clip);

    return clip;
  }

  /** @returns {object[]} все клипы */
  function getClips() {
    return SkinTool.model.state.animations;
  }

  /** @param {string} id */
  function getClip(id) {
    return getClips().find((clip) => clip.id === id) || null;
  }

  /** Клип для редактирования; если ничего не выбрано — первый. */
  function getCurrentClip() {
    const { state } = SkinTool.model;
    return getClip(state.currentAnimationId) || getClips()[0] || null;
  }

  /** @param {string} id */
  function setCurrentClip(id) {
    SkinTool.model.state.currentAnimationId = id;
  }

  // ---------- Дорожки ----------

  /**
   * Дорожка кости: kind — 'rotation' или 'position'.
   * @returns {object|null}
   */
  function createTrack(boneId, kind) {
    if (!PROPERTIES[kind]) return null;

    return {
      id: 'anim-track-' + (nextTrackId++),
      boneId,
      kind,
      interpolationMode: DEFAULT_INTERPOLATION,
      keys: []
    };
  }

  /** @param {object} clip @param {object} track */
  function addTrack(clip, track) {
    if (!clip || !track) return null;
    clip.tracks.push(track);
    return track;
  }

  /**
   * Удаляет дорожки костей, которых больше нет в скелете.
   * Вызывается после удаления костей, чтобы таймлайн не показывал пустых строк.
   */
  function pruneTracks() {
    const { state } = SkinTool.model;
    const alive = new Set(state.bones.map((bone) => bone.id));

    getClips().forEach((clip) => {
      clip.tracks = clip.tracks.filter((track) => alive.has(track.boneId));
    });
  }

  /** @returns {object|null} дорожка по boneId и виду свойства */
  function findTrack(clip, boneId, kind) {
    if (!clip) return null;
    return clip.tracks.find((track) => track.boneId === boneId && track.kind === kind) || null;
  }

  /** @returns {object[]} дорожки конкретной кости */
  function getBoneTracks(clip, boneId) {
    if (!clip) return [];
    return clip.tracks.filter((track) => track.boneId === boneId);
  }

  // ---------- Ключевые кадры ----------

  /**
   * Ставит кадр на время time: если там уже есть кадр — перезаписывает.
   * Кадры хранятся отсортированными по времени, потому что выборка
   * и экспорт идут по ним подряд.
   *
   * @param {object} track
   * @param {number} time — секунды от начала клипа
   * @param {number[]} value — градусы или мировые координаты
   * @returns {object} кадр
   */
  function setKey(track, time, value) {
    const existing = findKey(track, time);

    if (existing) {
      existing.value = value.slice();
      return existing;
    }

    const key = { time: Math.max(0, time), value: value.slice() };
    track.keys.push(key);
    track.keys.sort((a, b) => a.time - b.time);

    return key;
  }

  /** @returns {object|null} кадр на времени time */
  function findKey(track, time) {
    return track.keys.find((key) => Math.abs(key.time - time) <= TIME_EPSILON) || null;
  }

  /** @param {object} track @param {object} key */
  function removeKey(track, key) {
    const index = track.keys.indexOf(key);
    if (index >= 0) track.keys.splice(index, 1);
  }

  // ---------- Интерполяция ----------

  /**
   * Значение дорожки в момент времени — те же правила, что в движке
   * (AnimationTrackProperty.AdvancePlayback): между кадрами интерполируем
   * выбранным режимом, после последнего кадра держим последнее значение.
   *
   * @param {object} track
   * @param {number} time
   * @returns {number[]|null} null — времени ещё не наступило, кость в bind-посе
   */
  function sampleTrack(track, time) {
    const keys = track.keys;

    if (keys.length === 0) return null;

    // До первого кадра движок ничего не применяет — повторяем.
    if (time < keys[0].time - TIME_EPSILON) return null;

    const last = keys.length - 1;

    if (keys.length === 1 || time >= keys[last].time) return keys[last].value.slice();

    let index = 0;
    while (index < last && keys[index + 1].time <= time) index++;

    const a = keys[index];
    const b = keys[index + 1];
    const span = b.time - a.time;
    const t = span > 0 ? (time - a.time) / span : 1;

    switch (track.interpolationMode) {
      case 'Nearest':
        return (t < 0.5 ? a.value : b.value).slice();
      case 'Previous':
        return a.value.slice();
      case 'Linear':
        return lerp(a.value, b.value, t);
      default:
        return cubic(keys, index, t);
    }
  }

  /** @param {number[]} a @param {number[]} b @param {number} t */
  function lerp(a, b, t) {
    return [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t];
  }

  /**
   * Catmull-Rom по формуле VectorHelpers.InterpolateCubic из движка:
   * соседи за краями списка дублируются — так кривая не улетает.
   */
  function cubic(keys, index, t) {
    const a = keys[index].value;
    const b = keys[index + 1].value;
    const preA = (keys[index - 1] || keys[index]).value;
    const postB = (keys[index + 2] || keys[index + 1]).value;

    const result = [];

    for (let i = 0; i < 3; i++) {
      result.push(a[i] + (b[i] - preA[i] +
        (preA[i] * 2 - a[i] * 5 + b[i] * 4 - postB[i] +
          ((a[i] - b[i]) * 3 + postB[i] - preA[i]) * t) * t) * t * 0.5);
    }

    return result;
  }

  // ---------- Локальная система кости ----------

  /**
   * Мировой угол кадра → угол в системе родителя, градусы XYZ.
   *
   * Именно это значение движок получает в LocalAngleAsVector, и между
   * такими значениями он интерполирует. Вычитать углы Эйлера нельзя
   * (родитель повёрнут, и в local-системе оси другие), поэтому считаем
   * через кватернионы — тем же путём, что и экспорт в toYamlValue.
   *
   * @param {object|null} parentBone
   * @param {number[]} worldDegrees
   * @param {number[]} [referenceDegrees] — bind-угол самой кости в градусах
   * @returns {number[]} градусы XYZ
   */
  function toLocalRotation(parentBone, worldDegrees, referenceDegrees) {
    // Родитель не повёрнут → его система координат совпадает с мировой.
    // Возвращаем число как есть: конверсия не нужна, и текст не поедет.
    if (!parentBone || isIdentityRotation(parentBone.rotation)) {
      return worldDegrees.slice();
    }

    const world = quaternionFromDegrees(worldDegrees);
    const local = quaternionFromDegrees(parentBone.rotation).invert().multiply(world);

    return eulerFromQuaternion(local, referenceDegrees);
  }

  /**
   * Мировая позиция → LocalPosition в системе родителя.
   * @param {object|null} parentBone
   * @param {number[]} worldPosition
   * @returns {number[]}
   */
  function toLocalPosition(parentBone, worldPosition) {
    const local = new SkinTool.THREE.Vector3(
      worldPosition[0],
      worldPosition[1],
      worldPosition[2]
    );

    if (parentBone) {
      local.sub(new SkinTool.THREE.Vector3(
        parentBone.position[0],
        parentBone.position[1],
        parentBone.position[2]
      ));
      local.applyQuaternion(quaternionFromDegrees(parentBone.rotation).invert());
    }

    return [local.x, local.y, local.z];
  }

  /**
   * Дорожка, переведённая в локальную систему родителя.
   *
   * Кадры хранят мировые значения (так их удобно ставить инструментом),
   * но интерполировать движок будет локальные. Разница видна не только
   * на ключевых кадрах, но и между ними: у зеркальных костей вроде
   * right-shoulder локальная система развёрнута на 180° по Y, поэтому
   * мировая и локальная интерполяция расходятся до 180° посередине.
   *
   * @param {object} track
   * @param {object} bone
   * @returns {object} дорожка с локальными значениями
   */
  function asLocalTrack(track, bone) {
    const parent = bone.parentId === undefined || bone.parentId === null
      ? null : SkinTool.model.getBone(bone.parentId);

    const convert = track.kind === 'position'
      ? (value) => toLocalPosition(parent, value)
      : (value) => toLocalRotation(parent, value, bindLocalDegrees(bone));

    return {
      interpolationMode: track.interpolationMode,
      keys: track.keys.map((key) => ({ time: key.time, value: convert(key.value) }))
    };
  }

  /**
   * Bind-поворот кости в её собственной системе, градусы.
   *
   * Опорный угол, к которому притягиваем ветвь разложения Эйлера. У
   * зеркальных костей bind — это (0, 180, 0), поэтому близкой к bind
   * оказывается авторская запись с нулевым pitch, а не -180°.
   *
   * @param {object} bone
   * @returns {number[]|null} null для корня
   */
  function bindLocalDegrees(bone) {
    const parent = bone.parentId === undefined || bone.parentId === null
      ? null : SkinTool.model.getBone(bone.parentId);

    if (!parent) return bone.rotation.slice();

    if (isIdentityRotation(parent.rotation)) return bone.rotation.slice();

    const bindRotation = quaternionFromDegrees(bone.rotation);
    const bindLocal = quaternionFromDegrees(parent.rotation).invert().multiply(bindRotation);

    return eulerFromQuaternion(bindLocal);
  }

  /**
   * Кости в порядке обхода: родитель раньше детей.
   *
   * Мир ребёнка собирается из уже посчитанного мира родителя — так же,
   * как в движке, где локальные значения применяются к узлам дерева.
   *
   * @param {object[]} bones
   * @returns {object[]}
   */
  function hierarchyOrder(bones) {
    const byId = new Map(bones.map((bone) => [bone.id, bone]));
    const children = new Map();
    const parentOf = (bone) => (bone.parentId === undefined ? null : bone.parentId);

    bones.forEach((bone) => {
      const parentId = parentOf(bone);

      if (parentId === null || !byId.has(parentId)) return;

      if (!children.has(parentId)) children.set(parentId, []);
      children.get(parentId).push(bone);
    });

    const order = [];
    const visited = new Set();
    const walk = (bone) => {
      if (visited.has(bone.id)) return;
      visited.add(bone.id);
      order.push(bone);
      (children.get(bone.id) || []).forEach(walk);
    };

    bones.forEach((bone) => {
      const parentId = parentOf(bone);
      if (parentId === null || !byId.has(parentId)) walk(bone);
    });

    // Кости с битой ссылкой на родителя не должны потеряться.
    bones.forEach((bone) => {
      if (!visited.has(bone.id)) walk(bone);
    });

    return order;
  }

  /**
   * Позы всех дорожек клипа в момент времени.
   *
   * Здесь важны две системы отсчёта:
   *
   *   - движок интерполирует ЛОКАЛЬНЫЕ значения (LocalAngleAsVector,
   *     LocalPosition) и собирает мир как currentWorld(родитель) ∘ currentLocal,
   *     поэтому обход идёт по иерархии, а ключи пересчитываются в локальные
   *     через asLocalTrack — только так превью совпадает с экспортом;
   *   - state.pose деформатора — ДЕЛЬТА относительно bind-угла: модель
   *     нарисована в bind-посе, поэтому лишний поворот добавляется
   *     сверх bone.rotation (см. buildSkinMatrices), а позиция кадра
   *     приходит в деформатор мировым сдвигом от bind-координаты.
   *
   * @param {object} clip
   * @param {number} time
   * @returns {{pose: object, offsets: object}}
   */
  function sampleClip(clip, time) {
    const THREE = SkinTool.THREE;
    const { state, getBone } = SkinTool.model;
    const pose = {};
    const offsets = {};

    // У кости бывают обе дорожки, поэтому собираем их по костям.
    const tracks = new Map();

    clip.tracks.forEach((track) => {
      const bone = getBone(track.boneId);
      if (!bone) return;

      let entry = tracks.get(bone.id);

      if (!entry) {
        entry = {};
        tracks.set(bone.id, entry);
      }

      entry[track.kind] = track;
    });

    if (tracks.size === 0) return { pose, offsets };

    const world = new Map();

    hierarchyOrder(state.bones).forEach((bone) => {
      const entry = tracks.get(bone.id);
      const parent = bone.parentId === undefined || bone.parentId === null
        ? null : getBone(bone.parentId);
      const parentWorld = parent ? world.get(parent.id) : null;

      // bone.rotation и bone.position — мировые bind-значения скелета.
      const bindRotation = quaternionFromDegrees(bone.rotation);
      const bindPosition = new THREE.Vector3(
        bone.position[0],
        bone.position[1],
        bone.position[2]
      );
      const bindParent = parent
        ? quaternionFromDegrees(parent.rotation)
        : new THREE.Quaternion();
      const bindParentPosition = parent
        ? new THREE.Vector3(parent.position[0], parent.position[1], parent.position[2])
        : new THREE.Vector3();
      const bindLocal = bindParent.clone().invert().multiply(bindRotation);
      const bindLocalPosition = bindPosition.clone()
        .sub(bindParentPosition)
        .applyQuaternion(bindParent.clone().invert());

      // Мир без анимации — bind. Его считаем для всех костей, включая те,
      // у которых нет дорожек: иначе ребёнок собрался бы с пустым миром
      // родителя и потерял bind-поворот.
      const rotation = bindRotation.clone();
      const position = bindLocalPosition.clone();
      let localRotation = null;
      let localPosition = null;

      // Узел в движке всегда висит на родителе: world = parentPos + parentRot * local.
      // Даже без своей дорожки кость едет вместе с анимированным родителем.
      if (parentWorld) {
        position.applyQuaternion(parentWorld.rotation).add(parentWorld.position);
      }

      if (entry && entry.rotation) {
        const sampled = sampleTrack(asLocalTrack(entry.rotation, bone), time);

        if (sampled) {
          localRotation = sampled;
          rotation.copy(
            (parentWorld ? parentWorld.rotation : new THREE.Quaternion())
              .multiply(quaternionFromDegrees(sampled))
          );
        }
      }

      if (entry && entry.position) {
        const sampled = sampleTrack(asLocalTrack(entry.position, bone), time);

        if (sampled) {
          localPosition = sampled;
          const value = new THREE.Vector3(sampled[0], sampled[1], sampled[2]);

          if (parentWorld) {
            position.copy(value.applyQuaternion(parentWorld.rotation).add(parentWorld.position));
          } else {
            position.copy(value);
          }
        }
      }

      world.set(bone.id, { rotation, position });

      if (!entry) return;

      if (entry.rotation) {
        // Дельта, которую ждёт деформатор. Он композирует скин-матрицы по
        // иерархии (skin = skin_parent · own), а движок применяет к кости
        // абсолютную дельту currentWorld * inverse(bindWorld). Совпадение даёт
        // локальная дельта, перенесённая в bind-кадр родителя:
        //   delta = bindParent * (local * inverse(bindLocal)) * inverse(bindParent)
        // При неподвижном родителе (world = bind) сводится к привычному
        // world * inverse(bind), поэтому поведение не меняется.
        const local = localRotation !== null
          ? quaternionFromDegrees(localRotation)
          : bindLocal.clone();
        const delta = bindParent.clone()
          .multiply(local.multiply(bindLocal.clone().invert()))
          .multiply(bindParent.clone().invert());

        const euler = new THREE.Euler().setFromQuaternion(delta, 'XYZ');

        pose[bone.id] = [
          euler.x / DEG_TO_RAD,
          euler.y / DEG_TO_RAD,
          euler.z / DEG_TO_RAD
        ];
      }

      if (entry.position && localPosition !== null) {
        // Смещение сустава в локальной относительной форме. Деформатор
        // композирует матрицы, движок берёт смещение абсолютно
        // (currentWorldPosition - originalPosition), поэтому child's world
        // нужно перенести в bind-кадр родителя:
        //   offset = (bindParentPos - bindPos) + inverse(deltaParent) * (worldPos - worldParentPos)
        // При неподвижном родителе сводится к worldPos - bindPos.
        let offset = position.clone().sub(bindPosition);

        if (parentWorld) {
          const deltaParent = parentWorld.rotation
            .clone()
            .multiply(bindParent.clone().invert());
          const relative = position.clone()
            .sub(parentWorld.position)
            .applyQuaternion(deltaParent.invert());

          offset = bindParentPosition.clone().sub(bindPosition).add(relative);
        }

        offsets[bone.id] = [offset.x, offset.y, offset.z];
      }
    });

    return { pose, offsets };
  }

  /**
   * Дельта поворота: угол кадра относительно bind-угла.
   *
   * Порядок умножения здесь решает всё. Кадр хранит МИРОВОЙ поворот
   * кости, а кожа в движке берёт дельту как currentWorld * inverse(bindWorld)
   * (BoneSkinningSystem.ProceedBone), поэтому множитель получается
   * value * inverse(bind) — «справа налево».
   *
   * Обратный порядок (bind^-1 * value) совпадает с правильным только
   * у неповёрнутых костей. У зеркальных вроде right-shoulder с bind
   * 0,180,0 он поднимает руку вместо того, чтобы опустить, — а
   * quaternion-умножение некоммутативно, поэтому разница видна на
   * поворотах вокруг X и Z.
   *
   * @param {number[]} bindDegrees
   * @param {number[]} absoluteDegrees — мировой поворот кадра, градусы
   * @returns {number[]} дельта в градусах XYZ
   */
  function rotationDelta(bindDegrees, absoluteDegrees) {
    const delta = quaternionFromDegrees(absoluteDegrees)
      .multiply(quaternionFromDegrees(bindDegrees).invert());

    const euler = new SkinTool.THREE.Euler().setFromQuaternion(delta, 'XYZ');

    return [
      euler.x / DEG_TO_RAD,
      euler.y / DEG_TO_RAD,
      euler.z / DEG_TO_RAD
    ];
  }

  /**
   * Обратное преобразование: дельта позинга → угол кадра.
   * state.pose хранит дельту поверх bind, поэтому угол = delta * bind —
   * ровно то, что обращает rotationDelta.
   * @param {number[]} bindDegrees
   * @param {number[]} deltaDegrees
   * @returns {number[]} мировой угол в градусах XYZ
   */
  function rotationAbsolute(bindDegrees, deltaDegrees) {
    const absolute = quaternionFromDegrees(deltaDegrees).multiply(quaternionFromDegrees(bindDegrees));

    const euler = new SkinTool.THREE.Euler().setFromQuaternion(absolute, 'XYZ');

    return [
      euler.x / DEG_TO_RAD,
      euler.y / DEG_TO_RAD,
      euler.z / DEG_TO_RAD
    ];
  }

  /**
   * YAML со всеми клипами — такой файл можно положить в прототипы
   * и импортировать обратно без потерь.
   * @returns {string}
   */
  function toYamlAll() {
    return getClips().map((clip) => toYaml(clip)).join('');
  }

  // ---------- Экспорт ----------

  /**
   * YAML клипа в формате прототипа bodyAnimation.
   *
   * Два отличия от наших данных, о которых важно помнить:
   *   - keyTime — дельта от предыдущего кадра (у первого — от нуля);
   *   - поворот и позиция — в системе родителя: кости в движке действительно
   *     вложены (BoneSystem.CreateBone делает SetParent), а compound задаёт им
   *     мировые координаты, поэтому локальное значение получаем обратным
   *     преобразованием от мирового.
   *
   * @param {object} clip
   * @returns {string}
   */
  function toYaml(clip) {
    const { getBone } = SkinTool.model;
    const lines = [];

    lines.push('- type: bodyAnimation');
    lines.push('  id: ' + clip.id);
    lines.push('  animation:');
    lines.push('    length: ' + formatLength(clip.length));
    lines.push('    looped: ' + clip.looped);

    if (clip.tracks.length === 0) return lines.join('\n') + '\n';

    lines.push('    tracks:');

    clip.tracks.forEach((track) => {
      const bone = getBone(track.boneId);
      if (!bone) return;

      lines.push('      - bone: ' + bone.name);
      lines.push('        componentType: ' + COMPONENT_TYPE);
      lines.push('        property: ' + PROPERTIES[track.kind].property);
      lines.push('        interpolationMode: ' + track.interpolationMode);

      // Дорожка без кадров — валидное состояние (кость только что
      // переименовали и т. п.), но пустой список в YAML должен быть явным:
      // голое «keyFrames:» движок читает как null.
      if (track.keys.length === 0) {
        lines.push('        keyFrames: []');
        return;
      }

      lines.push('        keyFrames:');

      let previousTime = 0;

      track.keys.forEach((key) => {
        const delta = key.time - previousTime;
        previousTime = key.time;
        lines.push('          - keyTime: ' + formatSeconds(delta));
        lines.push('            value: ' + toYamlValue(track, key.value, bone).join(','));
      });
    });

    return lines.join('\n') + '\n';
  }

  /**
   * @param {object} track
   * @param {number[]} value — градусы или мировая позиция
   * @param {object} bone
   * @returns {number[]} значения в виде YAML
   */
  function toYamlValue(track, value, bone) {
    const parent = bone.parentId === null || bone.parentId === undefined
      ? null : SkinTool.model.getBone(bone.parentId);

    // Пересчёт в локальную систему родителя общий с сэмплером
    // (toLocalRotation / toLocalPosition): экспорт и превью обязаны
    // считать кадр одинаково, иначе анимация в движке поедет.
    if (track.kind !== 'position') {
      // Градусы → радианы: в YAML движка углы именно в радианах.
      return toLocalRotation(parent, value, bindLocalDegrees(bone))
        .map((angle) => round(angle * DEG_TO_RAD));
    }

    return toLocalPosition(parent, value).map(round);
  }

  /** @param {number[]} degrees */
  function quaternionFromDegrees(degrees) {
    const THREE = SkinTool.THREE;
    const euler = new THREE.Euler(
      THREE.MathUtils.degToRad(degrees[0]),
      THREE.MathUtils.degToRad(degrees[1]),
      THREE.MathUtils.degToRad(degrees[2]),
      'XYZ'
    );

    return new THREE.Quaternion().setFromEuler(euler);
  }

  /**
   * Тождественный ли поворот?
   *
   * Если поворот bind-родителя тождественный, его система координат совпадает
   * с мировой: локальный угол из YAML равен мировому без всякой конверсии.
   * Это не только быстрее — главное, что так число из файла доходит до
   * экспорта дословно. Через кватернион пришлось бы выбирать ветвь разложения
   * Эйлера, и авторское «0, 3.83, -1.134» превратилось бы в
   * «-3.1416, -0.6884, 2.0076» — то же вращение, но другой текст.
   *
   * @param {number[]|null|undefined} degrees
   * @returns {boolean}
   */
  function isIdentityRotation(degrees) {
    return !degrees
      || degrees.every((value) => Math.abs(value) <= 1e-9);
  }

  /** @param {number[]} radians */
  function quaternionFromRadians(radians) {
    const THREE = SkinTool.THREE;

    return new THREE.Quaternion().setFromEuler(new THREE.Euler(
      radians[0],
      radians[1],
      radians[2],
      'XYZ'
    ));
  }

  /**
   * Углы XYZ для поворота — ближайшие к опорным.
   *
   * setFromQuaternion возвращает только одну запись, и для наших костей
   * она часто неудобная: у зеркального right-shoulder pitch скачет к -180°,
   * хотя автор писал 0. Держим список эквивалентных записей (поворот тот же,
   * числа другие), сверяем каждую обратным ходом через кватернион и берём
   * ту, что ближе всего к опорному углу.
   *
   * @param {THREE.Quaternion} quaternion
   * @param {number[]} [referenceDegrees] — опорный угол, обычно bind кости
   * @returns {number[]} градусы XYZ
   */
  function eulerFromQuaternion(quaternion, referenceDegrees) {
    const THREE = SkinTool.THREE;
    const principal = new THREE.Euler().setFromQuaternion(quaternion, 'XYZ');
    const base = [principal.x, principal.y, principal.z];
    const pi = Math.PI;
    const reference = referenceDegrees || [0, 0, 0];

    // Сдвиги, которые переводят одну и ту же матрицу в другую запись.
    // Половина (±π на трёх осях) и целые обороты — это все варианты, где
    // atan2 может вернуть −π вместо +π.
    const shifts = [
      [0, 0, 0],
      [pi, pi, pi],
      [-pi, -pi, -pi],
      [pi, 0, pi],
      [-pi, 0, -pi],
      [0, pi, pi],
      [0, -pi, -pi],
      [2 * pi, 0, 0],
      [-2 * pi, 0, 0],
      [0, 2 * pi, 0],
      [0, -2 * pi, 0],
      [0, 0, 2 * pi],
      [0, 0, -2 * pi]
    ];

    let best = base.map((value) => value / DEG_TO_RAD);
    let bestDistance = best.reduce((sum, value, index) => (
      sum + Math.pow(value - reference[index], 2)
    ), 0);

    shifts.forEach((shift) => {
      const candidate = [
        base[0] + shift[0],
        base[1] + shift[1],
        base[2] + shift[2]
      ];

      // Проверяем обратным ходом: лишние варианты просто отсеются.
      if (Math.abs(quaternionFromRadians(candidate).dot(quaternion)) < 1 - 1e-9) return;

      const degrees = candidate.map((value) => value / DEG_TO_RAD);
      const distance = degrees.reduce((sum, value, index) => (
        sum + Math.pow(value - reference[index], 2)
      ), 0);

      if (distance < bestDistance) {
        bestDistance = distance;
        best = degrees;
      }
    });

    return best;
  }

  /** Длина клипа: движок ждёт TimeSpan, поэтому "1s". */
  function formatLength(seconds) {
    return round(seconds) + 's';
  }

  function formatSeconds(seconds) {
    // Дельты кадров в движке читаются как float, поэтому не округляем
    // до целых: 0.25 и 0.3 значат разное.
    return String(Number(seconds.toFixed(4)));
  }

  /** @param {number} value */
  function round(value) {
    return Number(value.toFixed(4));
  }

  SkinTool.animation = {
    INTERPOLATION_MODES,
    DEFAULT_INTERPOLATION,
    ENGINE_DEFAULT_INTERPOLATION,
    COMPONENT_TYPE,
    PROPERTIES,
    createClip,
    nextClipId,
    addClip,
    getClips,
    getClip,
    getCurrentClip,
    setCurrentClip,
    createTrack,
    addTrack,
    findTrack,
    getBoneTracks,
    pruneTracks,
    setKey,
    findKey,
    removeKey,
    sampleTrack,
    sampleClip,
    rotationDelta,
    rotationAbsolute,
    isIdentityRotation,
    eulerFromQuaternion,
    bindLocalDegrees,
    toYaml,
    toYamlAll
  };
})(window.SkinTool);
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

  /** Движок по умолчанию читает трек как Cubic — повторяем. */
  const DEFAULT_INTERPOLATION = 'Cubic';

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

  /**
   * Позы всех дорожек клипа в момент времени.
   *
   * Здесь важна разница двух систем отсчёта:
   *
   *   - кадр анимации — АБСОЛЮТНЫЙ угол кости (именно его пишет движок
   *     в LocalAngleAsVector, заменяя текущий угол целиком);
   *   - state.pose деформатора — ДЕЛЬТА относительно bind-угла: модель
   *     нарисована в bind-посе, поэтому лишний поворот добавляется
   *     сверх bone.rotation (см. buildSkinMatrices).
   *
   * Поэтому абсолютный кадр переводится в дельту через кватернионы:
   * delta = inverse(bind) * absolute. Для костей с нулевым bind-углом
   * это тождество, а для правой стороны (bind 0,180,0) — необходимое
   * преобразование, иначе поворот применился бы дважды.
   *
   * Позиция кадра — мировая, а деформатору нужен сдвиг относительно
   * bind-координаты.
   *
   * @param {object} clip
   * @param {number} time
   * @returns {{pose: object, offsets: object}}
   */
  function sampleClip(clip, time) {
    const pose = {};
    const offsets = {};

    clip.tracks.forEach((track) => {
      const bone = SkinTool.model.getBone(track.boneId);
      if (!bone) return;

      const value = sampleTrack(track, time);
      if (!value) return;

      if (track.kind === 'rotation') {
        pose[track.boneId] = rotationDelta(bone.rotation, value);
      } else {
        offsets[track.boneId] = [
          value[0] - bone.position[0],
          value[1] - bone.position[1],
          value[2] - bone.position[2]
        ];
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
    const parent = bone.parentId === null ? null : SkinTool.model.getBone(bone.parentId);

    if (track.kind !== 'position') {
      // Локальный угол: мировой минус родительский. Вычитание углов Эйлера
      // точно только когда поворот вокруг одной оси (как у большинства костей),
      // поэтому общий случай считаем через кватернионы.
      const world = quaternionFromDegrees(value);
      const local = parent
        ? quaternionFromDegrees(parent.rotation).invert().multiply(world)
        : world;

      const euler = new SkinTool.THREE.Euler().setFromQuaternion(local, 'XYZ');

      // setFromQuaternion уже отдаёт радианы — движок ждёт именно их,
      // поэтому конвертировать здесь больше нечего.
      return [
        round(euler.x),
        round(euler.y),
        round(euler.z)
      ];
    }

    // LocalPosition — положение в системе родителя, а не в мире: сначала
    // убираем начало родителя, потом его поворот.
    const world = new SkinTool.THREE.Vector3(value[0], value[1], value[2]);

    if (parent) {
      world.sub(new SkinTool.THREE.Vector3(
        parent.position[0],
        parent.position[1],
        parent.position[2]
      ));
      world.applyQuaternion(quaternionFromDegrees(parent.rotation).invert());
    }

    return [round(world.x), round(world.y), round(world.z)];
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
    toYaml,
    toYamlAll
  };
})(window.SkinTool);
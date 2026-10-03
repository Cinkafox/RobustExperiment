/**
 * Импорт анимаций из YAML в формате прототипов движка
 * (`- type: bodyAnimation` с секцией `animation`).
 *
 * Формат на входе совпадает с тем, что пишет SkinTool.animation.toYaml,
 * поэтому экспорт → импорт проходит без потерь. Чужой файл прототипов
 * тоже читается: у каждой сущности может быть свой `id`, а
 * `interpolationMode`/`looped`/`length` могут отсутствовать — тогда
 * берём значения движка по умолчанию.
 *
 * Кости ищутся по имени: в файле движка дорожка знает только строку
 * `bone`, а в инструменте дорожка привязана к boneId.
 */
(function (SkinTool) {
  'use strict';

  const COMPONENT_TYPE = 'Content.Shared.Transform.Transform3dComponent';

  /** Свойство YAML → вид дорожки в инструменте. */
  const PROPERTY_KINDS = {
    LocalAngleAsVector: 'rotation',
    LocalAngle: 'rotation',
    LocalPosition: 'position',
    Position: 'position'
  };

  const RAD_TO_DEG = 180 / Math.PI;

  /**
   * @param {string} text
   * @returns {{clips: object[], skipped: string[]}}
   */
  function parseAnimations(text) {
    const documents = SkinTool.yamlParser.parse(text);
    const clips = [];
    const skipped = [];
    const { state } = SkinTool.model;

    const bonesByName = new Map(state.bones.map((bone) => [bone.name, bone]));

    for (const document of documents) {
      const entities = Array.isArray(document) ? document : [document];

      for (const entity of entities) {
        if (!entity || typeof entity !== 'object') continue;
        if (entity.type !== 'bodyAnimation') continue;

        const id = String(entity.id || SkinTool.animation.nextClipId());
        const animation = entity.animation || {};

        const clip = SkinTool.animation.createClip(id, {
          length: parseLength(animation.length),
          looped: animation.looped !== false
        });

        const tracks = Array.isArray(animation.tracks) ? animation.tracks : [];

        tracks.forEach((rawTrack) => {
          const kind = PROPERTY_KINDS[rawTrack.property];
          const bone = bonesByName.get(String(rawTrack.bone));

          if (!kind) {
            skipped.push(`${id}: свойство ${rawTrack.property}`);
            return;
          }

          if (!bone) {
            skipped.push(`${id}: кость ${rawTrack.bone}`);
            return;
          }

          const track = SkinTool.animation.addTrack(
            clip,
            SkinTool.animation.createTrack(bone.id, kind)
          );

          track.interpolationMode = normalizeMode(rawTrack.interpolationMode);

          const keys = Array.isArray(rawTrack.keyFrames) ? rawTrack.keyFrames : [];
          let absoluteTime = 0;

          keys.forEach((rawKey) => {
            // keyTime в движке — дельта от предыдущего кадра.
            absoluteTime += parseNumber(rawKey.keyTime);

            const value = parseVector(rawKey.value);

            if (value) {
              // Поворот в YAML — локальный, а храним мы мировой.
              SkinTool.animation.setKey(
                track,
                absoluteTime,
                kind === 'rotation'
                  ? toWorldRotation(bone, value)
                  : toWorldPosition(bone, value)
              );
            }
          });
        });

        clips.push(clip);
      }
    }

    return { clips, skipped };
  }

  /**
   * Разбор файла и добавление клипов к текущим.
   * @param {string} text
   * @returns {{added: number, replaced: number, skipped: string[]}}
   */
  function importYAML(text) {
    const { clips, skipped } = parseAnimations(text);
    const { state } = SkinTool.model;
    let replaced = 0;

    clips.forEach((clip) => {
      const index = state.animations.findIndex((existing) => existing.id === clip.id);

      if (index >= 0) {
        state.animations[index] = clip;
        replaced++;
      } else {
        state.animations.push(clip);
      }
    });

    if (clips.length > 0) state.currentAnimationId = clips[0].id;

    return { added: clips.length - replaced, replaced, skipped };
  }

  /**
   * Скаляр из YAML: парсер отдаёт всё строками.
   * @param {*} value
   * @param {number} fallback
   */
  function parseNumber(value, fallback) {
    const number = Number(value);
    return Number.isFinite(number) ? number : (fallback === undefined ? 0 : fallback);
  }

  /**
   * Длительность клипа: TimeSpan приходит как "1s" / "00:00:01".
   * @param {*} value
   * @returns {number} секунды
   */
  function parseLength(value) {
    if (typeof value === 'number' && Number.isFinite(value)) return value;

    const text = String(value === undefined || value === null ? '' : value).trim();
    if (!text) return 1;

    // "hh:mm:ss(.fff)" — формат TimeSpan.
    if (text.includes(':')) {
      // Явно оборачиваем: map передаёт индекс вторым аргументом, а он
      // ушёл бы в fallback parseNumber.
      const parts = text.split(':').map((part) => parseNumber(part));
      const seconds = parts.reduce((total, part) => total * 60 + part, 0);

      if (parts.length === 3) return Number(seconds.toFixed(4));
      if (parts.length === 2) return Number((parts[0] * 60 + parts[1]).toFixed(4));
    }

    const number = parseNumber(text.replace(/s$/i, ''), NaN);

    return Number.isFinite(number) && number > 0 ? number : 1;
  }

  /**
   * @param {*} value — "0.1,0.2,0.3" или массив
   * @returns {number[]|null}
   */
  function parseVector(value) {
    const parts = Array.isArray(value)
      ? value
      : String(value === undefined || value === null ? '' : value).split(',');

    const numbers = parts.map((part) => parseNumber(part));

    return numbers.length >= 3 && numbers.every(Number.isFinite) ? numbers.slice(0, 3) : null;
  }

  /**
   * @param {*} value
   * @returns {string} один из режимов интерполации движка
   */
  function normalizeMode(value) {
    const mode = String(value || '');

    if (SkinTool.animation.INTERPOLATION_MODES.indexOf(mode) >= 0) return mode;

    // Поля нет — значит файл рассчитан на дефолт движка (Linear), а не на
    // авторский дефолт инструмента: иначе превью разойдётся с игрой.
    return SkinTool.animation.ENGINE_DEFAULT_INTERPOLATION;
  }

  /**
   * Локальный угол из YAML → мировой поворот кости в градусах.
   *
   * В движке LocalAngleAsVector — поворот относительно родителя, поэтому
   * мир получается умножением bind-поворота родителя СЛЕВА. Композицию
   * считаем здесь, а не через rotationAbsolute: та строит «дельта поверх
   * bind» (delta * bind) и для этой задачи не подходит.
   * Порядок осей тот же, что при экспорте: XYZ, радианы → градусы.
   * @param {object} bone
   * @param {number[]} local — радианы
   * @returns {number[]} градусы
   */
  function toWorldRotation(bone, local) {
    const THREE = SkinTool.THREE;
    const degrees = [local[0] * RAD_TO_DEG, local[1] * RAD_TO_DEG, local[2] * RAD_TO_DEG];
    const parent = bone.parentId === null ? null : SkinTool.model.getBone(bone.parentId);

    // Родитель не повёрнут → локальный угол из YAML и есть мировой.
    // Только переводим единицы, кватернион не трогаем: так число из файла
    // доживает до экспорта дословно. Через кватернион пришлось бы выбирать
    // ветвь разложения, и 0 превратился бы в -180 у зеркальных костей.
    if (!parent || SkinTool.animation.isIdentityRotation(parent.rotation)) {
      return degrees;
    }

    const world = new THREE.Quaternion().setFromEuler(new THREE.Euler(
      THREE.MathUtils.degToRad(degrees[0]),
      THREE.MathUtils.degToRad(degrees[1]),
      THREE.MathUtils.degToRad(degrees[2]),
      SkinTool.EULER_ORDER
    ));

    world.premultiply(new THREE.Quaternion().setFromEuler(new THREE.Euler(
      THREE.MathUtils.degToRad(parent.rotation[0]),
      THREE.MathUtils.degToRad(parent.rotation[1]),
      THREE.MathUtils.degToRad(parent.rotation[2]),
      SkinTool.EULER_ORDER
    )));

    // Опорный угол — bind кости в градусах, чтобы выбрать читаемую запись.
    return SkinTool.animation.eulerFromQuaternion(
      world,
      bone.rotation
    );
  }

  /**
   * LocalPosition → мировая позиция кости.
   * @param {object} bone
   * @param {number[]} local
   * @returns {number[]}
   */
  function toWorldPosition(bone, local) {
    const parent = bone.parentId === null ? null : SkinTool.model.getBone(bone.parentId);

    return parent ? rotateAndTranslate(local, parent) : [local[0], local[1], local[2]];
  }

  /**
   * Мировая позиция из локальной: поворот родителя, затем сдвиг к нему.
   * @param {number[]} local
   * @param {object} parent
   * @returns {number[]}
   */
  function rotateAndTranslate(local, parent) {
    const THREE = SkinTool.THREE;
    const vector = new THREE.Vector3(local[0], local[1], local[2]);
    const parentQuaternion = new THREE.Quaternion().setFromEuler(new THREE.Euler(
      THREE.MathUtils.degToRad(parent.rotation[0]),
      THREE.MathUtils.degToRad(parent.rotation[1]),
      THREE.MathUtils.degToRad(parent.rotation[2]),
      SkinTool.EULER_ORDER
    ));

    vector.applyQuaternion(parentQuaternion);
    vector.x += parent.position[0];
    vector.y += parent.position[1];
    vector.z += parent.position[2];

    return [vector.x, vector.y, vector.z];
  }

  /**
   * Имя файла для выгрузки клипа.
   * @param {object} clip
   * @returns {string}
   */
  function buildFileName(clip) {
    return (clip.id || 'bodyAnimation').replace(/[^\w.-]+/g, '_') + '.yml';
  }

  SkinTool.animationYaml = {
    COMPONENT_TYPE,
    parseAnimations,
    importYAML,
    buildFileName
  };
})(window.SkinTool);
/**
 * Импорт скелета из YAML (формат entity / BoneCompound).
 *
 * Разбор текста делегирован SkinTool.yamlParser, здесь только поиск
 * нужного компонента и построение списка костей.
 */
(function (SkinTool) {
  'use strict';

  function bind() {
    SkinTool.dom.bindDropZone('importDrop', 'yamlInput', readFile);
  }

  /** @param {File} file */
  function readFile(file) {
    const reader = new FileReader();
    reader.onload = (event) => {
      try {
        importYAML(event.target.result);
        SkinTool.dom.setStatus(`Импортировано ${SkinTool.model.state.bones.length} костей из ${file.name}`);
      } catch (error) {
        alert('Ошибка импорта YAML: ' + error.message);
        console.error(error);
      }
    };
    reader.readAsText(file);
  }

  /**
   * Ищет компонент BoneCompound в файле.
   *
   * Формат файлов-прототипов допускает два варианта:
   *   - корень — отображение "type: entity" (в таком виде инструмент
   *     сам экспортирует);
   *   - корень — список сущностей "- type: entity" (так лежат файлы
   *     в Resources/Prototypes/Entities), причём сущностей может быть
   *     несколько, а нужная — не обязательно первая.
   *
   * @param {string} text
   * @returns {{entity: object, compound: object}}
   */
  function findSkeleton(text) {
    const documents = SkinTool.yamlParser.parse(text);
    let referencedEntityId = null;

    for (const document of documents) {
      const entities = Array.isArray(document) ? document : [document];

      for (const entity of entities) {
        if (!entity || typeof entity !== 'object') continue;

        const rawComponents = entity.components;
        const components = Array.isArray(rawComponents)
          ? rawComponents
          : rawComponents && typeof rawComponents === 'object'
            ? [rawComponents]
            : [];

        const boneComponent = components.find((component) =>
          component && typeof component === 'object' && component.type === 'BoneCompound' && component.compound
        ) || components.find((component) =>
          component && typeof component === 'object' && component.compound
        );

        if (boneComponent) return { entity, compound: boneComponent.compound };

        // Компонент без данных: скелет лежит в отдельной сущности.
        const link = components.find((component) =>
          component && typeof component === 'object' && component.compoundEntId
        );
        if (link) referencedEntityId = link.compoundEntId;
      }
    }

    if (referencedEntityId) {
      throw new Error(
        'BoneCompound ссылается на сущность "' + referencedEntityId +
        '" — импортируйте YAML-файл с этой сущностью'
      );
    }

    throw new Error('Не найден BoneCompound в YAML');
  }

  /**
   * Разбирает список костей (рекурсивно по child) и заменяет текущий скелет.
   * @param {string} text
   */
  function importYAML(text) {
    const { state } = SkinTool.model;
    const { entity, compound } = findSkeleton(text);

    if (entity.id) {
      state.entityId = entity.id;
      SkinTool.dom.$('entityId').value = entity.id;
    }

    const newBones = [];
    let maxId = 0;

    function collectBone(obj, parentId) {
      if (!obj || obj.position === undefined) return;

      const position = String(obj.position).split(',').map((v) => parseFloat(v.trim()));
      const rotation = String(obj.rotation || '0,0,0').split(',').map((v) => parseFloat(v.trim()));
      const id = ++maxId;

      const indices = [];
      const weights = [];
      if (obj.data && Array.isArray(obj.data)) {
        obj.data.forEach((entry) => {
          const index = parseInt(entry.boneIndices, 10);
          if (!isNaN(index)) {
            const weight = parseFloat(entry.boneWeights);
            indices.push(index);
            weights.push(isNaN(weight) ? 1 : weight);
          }
        });
      }

      newBones.push({
        id,
        name: obj.name || 'bone',
        position: position.length >= 3 ? position : [0, 0, 0],
        rotation: rotation.length >= 3 ? rotation : [0, 0, 0],
        indices,
        weights,
        parentId
      });

      if (obj.child) {
        const children = Array.isArray(obj.child) ? obj.child : [obj.child];
        children.forEach((child) => collectBone(child, id));
      }
    }

    collectBone(compound, null);

    if (newBones.length === 0) {
      throw new Error('В YAML не найдено ни одной кости');
    }

    state.bones = newBones;
    state.nextBoneId = maxId + 1;
    state.selectedBoneId = newBones[0].id;

    SkinTool.bones.renderBonesList();
    SkinTool.bones.renderBoneProps();
    SkinTool.bones.renderIndicesList();
    SkinTool.colors.updatePointColors();
    SkinTool.deformer.rebuild();
    SkinTool.anchors.sync();
    SkinTool.skinning.updateTab();
    SkinTool.pose.updateTab();
  }

  SkinTool.yamlImport = { bind, findSkeleton, importYAML };
})(window.SkinTool);
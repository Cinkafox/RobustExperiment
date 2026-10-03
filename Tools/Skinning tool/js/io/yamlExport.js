/**
 * Экспорт скелета в YAML формата entity / BoneCompound.
 */
(function (SkinTool) {
  'use strict';

  const FALLBACK_ENTITY_ID = 'skeleton';

  function bind() {
    SkinTool.dom.$('btnExport').addEventListener('click', showInPreview);
    SkinTool.dom.$('btnCopy').addEventListener('click', copy);
    SkinTool.dom.$('btnDownload').addEventListener('click', download);
  }

  function getEntityId() {
    return SkinTool.dom.$('entityId').value || FALLBACK_ENTITY_ID;
  }

  /**
   * @returns {string} YAML-текст скелета
   */
  function buildYAML() {
    const { getChildBones, getSkinningData } = SkinTool.model;
    const roots = getChildBones(null);
    const indent = (depth) => '  '.repeat(depth);

    function renderBone(bone, depth) {
      let out = '';
      out += indent(depth) + `- position: ${formatVector(bone.position, 2)}\n`;
      out += indent(depth) + `  rotation: ${formatVector(bone.rotation)}\n`;
      out += indent(depth) + `  name: ${bone.name}\n`;

      const skinning = getSkinningData(bone).sort((a, b) => a.index - b.index);
      if (skinning.length > 0) {
        out += indent(depth) + `  data:\n`;
        skinning.forEach(({ index, weight }) => {
          out += indent(depth + 2) + `- boneIndices: ${index}\n`;
          out += indent(depth + 3) + `boneWeights: ${formatWeight(weight)}\n`;
        });
      }

      const children = getChildBones(bone.id);
      if (children.length > 0) {
        out += indent(depth) + `  child:\n`;
        children.forEach((child) => {
          out += renderBone(child, depth + 2);
        });
      }

      return out;
    }

    let out = '';
    out += `type: entity\n`;
    out += `id: ${getEntityId()}\n`;
    out += `components:\n`;
    out += `  - type: BoneCompound\n`;
    out += `    compound:\n`;

    if (roots.length === 0) {
      // Совсем пустой скелет — выгружаем заглушку, чтобы файл остался валидным.
      out += `      position: 0,0,0\n`;
      out += `      rotation: 0,0,0\n`;
      out += `      name: root\n`;
    } else if (roots.length === 1) {
      const root = roots[0];
      out += `      position: ${formatVector(root.position, 2)}\n`;
      out += `      rotation: ${formatVector(root.rotation)}\n`;
      out += `      name: ${root.name}\n`;

      const children = getChildBones(root.id);
      if (children.length > 0) {
        out += `      child:\n`;
        children.forEach((child) => {
          out += renderBone(child, 4);
        });
      } else {
        // У корня могут быть индексы и веса, даже когда нет потомков.
        const rootSkinning = getSkinningData(root).sort((a, b) => a.index - b.index);
        if (rootSkinning.length > 0) {
          out += `      data:\n`;
          rootSkinning.forEach(({ index, weight }) => {
            out += `        - boneIndices: ${index}\n`;
            out += `          boneWeights: ${formatWeight(weight)}\n`;
          });
        }
      }
    } else {
      // Несколько корневых костей — прячем их за общим root.
      out += `      position: 0,0,0\n`;
      out += `      rotation: 0,0,0\n`;
      out += `      name: root\n`;
      out += `      child:\n`;
      roots.forEach((root) => {
        out += renderBone(root, 4);
      });
    }

    return out;
  }

  /** @param {number[]} values */
  function formatVector(values, precision) {
    const parts = precision === undefined
      ? values
      : values.map((v) => v.toFixed(precision));
    return parts.join(',');
  }

  /** Вес вершины: целые — без дробей, дробные — с точностью до 3 знаков. */
  function formatWeight(weight) {
    return Number.isInteger(weight) ? String(weight) : String(Number(weight.toFixed(3)));
  }

  function showInPreview() {
    SkinTool.dom.$('yamlOutput').textContent = buildYAML();
  }

  function copy() {
    const yaml = buildYAML();
    SkinTool.dom.$('yamlOutput').textContent = yaml;

    navigator.clipboard.writeText(yaml)
      .then(() => SkinTool.dom.setStatus('YAML скопирован в буфер обмена'))
      .catch(() => SkinTool.dom.setStatus('Ошибка копирования'));
  }

  function download() {
    const yaml = buildYAML();
    SkinTool.dom.$('yamlOutput').textContent = yaml;

    const blob = new Blob([yaml], { type: 'text/yaml' });
    const url = URL.createObjectURL(blob);

    const link = document.createElement('a');
    link.href = url;
    link.download = getEntityId() + '.yml';
    link.click();

    URL.revokeObjectURL(url);
  }

  SkinTool.yamlExport = { bind, buildYAML };
})(window.SkinTool);
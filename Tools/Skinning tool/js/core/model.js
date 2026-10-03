/**
 * Модель данных: состояние инструмента, цвета костей и запросы к скелету.
 * Не знает ни о DOM, ни о Three.js-сцене — только про данные.
 */
(function (SkinTool) {
  'use strict';

  const state = {
    vertices: [],
    bones: [],
    selectedBoneId: null,
    entityId: 'alexandraSkeleton',
    selectionMode: 'none',
    isBoxSelectMode: false,
    showAnchors: true,
    showSkeleton: true,
    nextBoneId: 1,
    currentTab: 'grouping',
    skinningMode: 'bones',
    problems: [],
    /**
     * Пробная поза: { [boneId]: [rx, ry, rz] } в градусах.
     * Не попадает в экспорт, пока не нажата кнопка «Записать в кость».
     */
    pose: {},
    normalizeWeights: true
  };

  const BONE_COLORS = [
    0xff5555, 0x55ff55, 0x5555ff, 0xffff55,
    0xff55ff, 0x55ffff, 0xffaa55, 0xaaff55,
    0xaa55ff, 0xff55aa, 0x55ffaa, 0xff8888
  ];

  const UNASSIGNED_COLOR = 0x44ddff;

  /**
   * Оси вращения X/Y/Z. Один источник правды для ползунков позинга
   * (там берётся CSS-строка) и для окружностей в сцене (там — число).
   */
  const AXIS_COLORS = [0xff5555, 0x55ff55, 0x5555ff];

  /**
   * @param {number} boneId
   * @returns {THREE.Color}
   */
  function getBoneColor(boneId) {
    return new SkinTool.THREE.Color(BONE_COLORS[boneId % BONE_COLORS.length]);
  }

  /**
   * Тот же цвет, но в виде CSS-строки — для разметки списков.
   * @param {number} boneId
   * @returns {string}
   */
  function getBoneColorCss(boneId) {
    return '#' + getBoneColor(boneId).getHexString();
  }

  /**
   * @param {number|null} id
   * @returns {object|null}
   */
  function getBone(id) {
    if (id === null || id === undefined) return null;
    return state.bones.find((bone) => bone.id === id) || null;
  }

  /** @returns {object|null} */
  function getSelectedBone() {
    return getBone(state.selectedBoneId);
  }

  /**
   * @param {number|null} parentId — null для корневых костей
   * @returns {object[]}
   */
  function getChildBones(parentId) {
    return state.bones.filter((bone) => bone.parentId === parentId);
  }

  /**
   * Карта "индекс вершины -> список ID костей".
   * Вершина может принадлежать нескольким костям — это и считается проблемой.
   * @returns {Map<number, number[]>}
   */
  function buildVertexBoneMap() {
    const map = new Map();
    state.bones.forEach((bone) => {
      bone.indices.forEach((index) => {
        if (!map.has(index)) map.set(index, []);
        map.get(index).push(bone.id);
      });
    });
    return map;
  }

  /**
   * Вес вершины в кости. Веса хранятся параллельно bone.indices;
   * если вес не задан, вершина считается закреплённой жёстко (вес 1).
   *
   * @param {{indices:number[], weights?:number[]}} bone
   * @param {number} slot — позиция вершины в bone.indices
   * @returns {number}
   */
  function getWeight(bone, slot) {
    const weight = bone.weights && bone.weights[slot];
    return typeof weight === 'number' && isFinite(weight) ? weight : 1;
  }

  /**
   * Копия кости с уже разложенными весами — для экспорта.
   * @param {object} bone
   * @returns {{index:number, weight:number}[]}
   */
  function getSkinningData(bone) {
    return bone.indices.map((index, slot) => ({ index, weight: getWeight(bone, slot) }));
  }

  SkinTool.model = {
    state,
    BONE_COLORS,
    AXIS_COLORS,
    UNASSIGNED_COLOR,
    getBoneColor,
    getBoneColorCss,
    getBone,
    getSelectedBone,
    getChildBones,
    buildVertexBoneMap,
    getWeight,
    getSkinningData
  };
})(window.SkinTool);
/**
 * Раскраска вершин в облаке точек.
 *
 * Три режима визуализации:
 *   bones   — цвет кости, выбранная кость ярче остальных;
 *   problems — белая (нет кости), красная (несколько костей), зелёная (ок);
 *   heat    — чем больше костей влияет на вершину, тем «горячее» цвет.
 */
(function (SkinTool) {
  'use strict';

  const PROBLEM_COLORS = {
    unassigned: 0xffffff,
    multi: 0xff3030,
    single: 0x40ff40
  };

  const HEAT_COLORS = [
    0x444444, // 0 костей
    0x40a040, // 1 кость
    0xffcc00, // 2 кости
    0xff8000, // 3 кости
    0xff2020  // 4 и больше
  ];

  /**
   * Пересчитывает цвета всех вершин под текущий режим визуализации.
   */
  function updatePointColors() {
    const { state, UNASSIGNED_COLOR, buildVertexBoneMap, getBoneColor } = SkinTool.model;
    const THREE = SkinTool.THREE;

    const pointColors = SkinTool.scene.getPointColors();
    const points = SkinTool.scene.getPoints();
    if (!pointColors || !points) return;

    const vertexBoneMap = buildVertexBoneMap();
    const defaultColor = new THREE.Color(UNASSIGNED_COLOR);

    for (let i = 0; i < state.vertices.length; i++) {
      const boneIds = vertexBoneMap.get(i) || [];
      let color = defaultColor;

      if (state.skinningMode === 'bones') {
        if (boneIds.length > 0) {
          const ownerId = boneIds[0];
          color = getBoneColor(ownerId);
          if (ownerId === state.selectedBoneId) {
            color = color.clone().multiplyScalar(1.4);
            color.r = THREE.MathUtils.clamp(color.r, 0, 1);
            color.g = THREE.MathUtils.clamp(color.g, 0, 1);
            color.b = THREE.MathUtils.clamp(color.b, 0, 1);
          }
        }
      } else if (state.skinningMode === 'problems') {
        if (boneIds.length === 0) color = new THREE.Color(PROBLEM_COLORS.unassigned);
        else if (boneIds.length > 1) color = new THREE.Color(PROBLEM_COLORS.multi);
        else color = new THREE.Color(PROBLEM_COLORS.single);
      } else if (state.skinningMode === 'heat') {
        color = new THREE.Color(HEAT_COLORS[Math.min(boneIds.length, HEAT_COLORS.length - 1)]);
      }

      pointColors[i * 3] = color.r;
      pointColors[i * 3 + 1] = color.g;
      pointColors[i * 3 + 2] = color.b;
    }

    SkinTool.scene.markColorsDirty();
  }

  SkinTool.colors = { updatePointColors };
})(window.SkinTool);
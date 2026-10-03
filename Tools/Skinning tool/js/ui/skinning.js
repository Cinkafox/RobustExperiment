/**
 * Вкладка «Скиннинг»: статистика назначений, режимы визуализации,
 * поиск проблемных вершин и детали вершины под курсором.
 */
(function (SkinTool) {
  'use strict';

  const MODE_HINTS = {
    bones: 'Цвет = ID кости, яркая = выбранная',
    problems: '🔴 много костей, ⚪ не назначена, 🟢 ок',
    heat: 'Ярче = больше костей влияет на вершину'
  };

  function bind() {
    SkinTool.dom.$('vizBones').addEventListener('click', () => setVisualizationMode('bones'));
    SkinTool.dom.$('vizProblems').addEventListener('click', () => setVisualizationMode('problems'));
    SkinTool.dom.$('vizHeat').addEventListener('click', () => setVisualizationMode('heat'));
    SkinTool.dom.$('btnFindProblems').addEventListener('click', findProblems);
    SkinTool.dom.$('btnClearProblems').addEventListener('click', clearProblems);
  }

  /** @param {'bones'|'problems'|'heat'} mode */
  function setVisualizationMode(mode) {
    SkinTool.model.state.skinningMode = mode;

    SkinTool.dom.$('vizBones').classList.toggle('active', mode === 'bones');
    SkinTool.dom.$('vizProblems').classList.toggle('active', mode === 'problems');
    SkinTool.dom.$('vizHeat').classList.toggle('active', mode === 'heat');

    SkinTool.dom.$('vizHint').textContent = MODE_HINTS[mode];

    SkinTool.colors.updatePointColors();
  }

  /** Полностью перерисовывает содержимое вкладки. */
  function updateTab() {
    if (SkinTool.model.state.currentTab !== 'skinning') return;

    renderStats();
    renderBonesList();
    renderProblems();
  }

  function renderStats() {
    const { state, buildVertexBoneMap } = SkinTool.model;

    const total = state.vertices.length;
    const vertexBoneMap = buildVertexBoneMap();

    let unassigned = 0;
    let single = 0;
    let multi = 0;

    for (let i = 0; i < total; i++) {
      const boneIds = vertexBoneMap.get(i) || [];
      if (boneIds.length === 0) unassigned++;
      else if (boneIds.length === 1) single++;
      else multi++;
    }

    SkinTool.dom.$('skinningStats').innerHTML = `
      <div class="stat-row"><span class="stat-label">Всего вершин</span><span class="stat-value">${total}</span></div>
      <div class="stat-row ${single === total && total > 0 ? 'ok' : ''}"><span class="stat-label">Одна кость (ок)</span><span class="stat-value">${single}</span></div>
      <div class="stat-row ${unassigned > 0 ? 'problem' : 'ok'}"><span class="stat-label">Не назначено</span><span class="stat-value">${unassigned}</span></div>
      <div class="stat-row ${multi > 0 ? 'problem' : 'ok'}"><span class="stat-label">Несколько костей</span><span class="stat-value">${multi}</span></div>
      <div class="stat-row"><span class="stat-label">Костей всего</span><span class="stat-value">${state.bones.length}</span></div>
    `;
  }

  function renderBonesList() {
    const { state, getBoneColorCss } = SkinTool.model;
    const container = SkinTool.dom.$('skinningBonesList');

    if (state.bones.length === 0) {
      container.innerHTML = '<div class="placeholder-sm">Нет костей.</div>';
      return;
    }

    container.innerHTML = state.bones.map((bone) => {
      const selected = bone.id === state.selectedBoneId ? ' selected' : '';
      const color = getBoneColorCss(bone.id);

      return `<div class="bone-item${selected}" data-bone-id="${bone.id}">
        <div class="bone-content">
          <div class="bone-name">
            <span class="bone-color-dot" style="background:${color}"></span>
            ${bone.name}
          </div>
          <div class="bone-info">индексов: ${bone.indices.length}</div>
        </div>
      </div>`;
    }).join('');

    container.querySelectorAll('.bone-item').forEach((item) => {
      item.addEventListener('click', () => {
        SkinTool.bones.selectBone(parseInt(item.dataset.boneId));
      });
    });
  }

  /** Ищет вершины без кости и вершины с несколькими костями. */
  function findProblems() {
    const { state, buildVertexBoneMap } = SkinTool.model;
    const vertexBoneMap = buildVertexBoneMap();

    state.problems = [];
    for (let i = 0; i < state.vertices.length; i++) {
      const boneIds = vertexBoneMap.get(i) || [];
      if (boneIds.length === 0) {
        state.problems.push({ idx: i, type: 'unassigned', bones: boneIds });
      } else if (boneIds.length > 1) {
        state.problems.push({ idx: i, type: 'multi', bones: boneIds });
      }
    }

    renderProblems();
    SkinTool.dom.setStatus(`Найдено проблем: ${state.problems.length}`);
  }

  function clearProblems() {
    SkinTool.model.state.problems = [];
    renderProblems();
  }

  function renderProblems() {
    const { state, getBone } = SkinTool.model;
    const container = SkinTool.dom.$('problemsList');

    if (state.problems.length === 0) {
      container.innerHTML = '<div class="placeholder-sm">Проблем не найдено. Нажмите «Найти» для проверки.</div>';
      return;
    }

    container.innerHTML = state.problems.map((problem) => {
      const isUnassigned = problem.type === 'unassigned';
      const icon = isUnassigned ? '⚪' : '🔴';
      const label = isUnassigned ? 'не назначена' : `${problem.bones.length} костей`;
      const boneNames = problem.bones.map((id) => getBone(id)?.name || '?').join(', ') || '—';

      return `<div class="problem-item ${isUnassigned ? 'warn' : ''}" data-idx="${problem.idx}">
        <div>${icon} <b>Вершина #${problem.idx}</b> — ${label}</div>
        <div class="problem-bones">Кости: ${boneNames}</div>
      </div>`;
    }).join('');

    container.querySelectorAll('.problem-item').forEach((item) => {
      item.addEventListener('click', () => {
        const index = parseInt(item.dataset.idx);
        focusVertex(index);
        showVertexDetails(index);
      });
    });
  }

  /** @param {number} index */
  function focusVertex(index) {
    const vertex = SkinTool.scene.getVertexPosition(index);
    if (!vertex) return;

    SkinTool.scene.focusOnVertex(vertex);
    SkinTool.scene.flashVertex(index);
  }

  /** Заполняет блок «Детали вершины». @param {number} index */
  function showVertexDetails(index) {
    const { state, getBone, buildVertexBoneMap } = SkinTool.model;
    const container = SkinTool.dom.$('vertexDetails');

    const vertex = SkinTool.scene.getVertexPosition(index);
    if (!container || !vertex) return;

    const boneIds = buildVertexBoneMap().get(index) || [];

    let html = `<div class="vertex-header">
      <b>Вершина #${index}</b><br>
      pos: [${vertex.x.toFixed(3)}, ${vertex.y.toFixed(3)}, ${vertex.z.toFixed(3)}]
    </div>`;

    if (boneIds.length === 0) {
      html += '<div class="vertex-bad">⚠ Не назначена ни одной кости</div>';
    } else if (boneIds.length === 1) {
      html += `<div class="vertex-ok">✓ Одна кость: <b>${getBone(boneIds[0])?.name || '?'}</b></div>`;
    } else {
      html += `<div class="vertex-bad">⚠ Назначена ${boneIds.length} костям:</div>`;
      boneIds.forEach((boneId) => {
        html += `<div class="vertex-bone-item">• ${getBone(boneId)?.name || 'ID ' + boneId}</div>`;
      });
    }

    container.innerHTML = html;
  }

  SkinTool.skinning = {
    bind,
    setVisualizationMode,
    updateTab,
    findProblems,
    clearProblems,
    focusVertex,
    showVertexDetails
  };
})(window.SkinTool);
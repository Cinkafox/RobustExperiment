/**
 * Вкладка «Позинг»: проверка скелета вращением и редактирование весов.
 *
 * Пробная поза живёт в state.pose и деформирует модель через
 * SkinTool.deformer, но не меняет данные костей — пока пользователь
 * не нажмёт «Записать в кость», после чего поворот попадает в bone.rotation
 * и, соответственно, в экспорт.
 *
 * Правая панель показывает текущие значения позы и список весов
 * вершин выбранной кости с возможностью правки.
 */
(function (SkinTool) {
  'use strict';

  // Цвета берём из общей палитры осей — чтобы кольца в сцене и ползунки
  // в панели всегда совпадали.
  const AXES = [
    { key: 'x', label: 'X' },
    { key: 'y', label: 'Y' },
    { key: 'z', label: 'Z' }
  ].map(function (axis, index) {
    axis.color = '#' + SkinTool.model.AXIS_COLORS[index].toString(16).padStart(6, '0');
    return axis;
  });

  const MIN_ANGLE = -180;
  const MAX_ANGLE = 180;

  /** Подпись дерева костей, чтобы в списке совпадать с панелью группировки. */
  let boneListSignature = '';

  // ---------- Разметка ----------

  function bind() {
    buildRotationControls();
    buildBoneSelect();
    buildWeightHeader();

    SkinTool.dom.$('btnPoseApply').addEventListener('click', () => {
      SkinTool.deformer.applyPose();
      updateTab();
      SkinTool.bones.renderBonesList();
      SkinTool.bones.renderBoneProps();
      SkinTool.anchors.sync();
      SkinTool.dom.setStatus('Пробная поза записана в кости');
    });

    SkinTool.dom.$('btnPoseDiscard').addEventListener('click', () => {
      const bone = SkinTool.model.getSelectedBone();
      SkinTool.deformer.discardPose(bone ? bone.id : null);
      updateTab();
      SkinTool.anchors.sync();
      SkinTool.dom.setStatus('Пробная поза сброшена');
    });

    SkinTool.dom.$('btnPoseDiscardAll').addEventListener('click', () => {
      SkinTool.deformer.discardPose(null);
      updateTab();
      SkinTool.anchors.sync();
      SkinTool.dom.setStatus('Все пробные позы сброшены');
    });

    SkinTool.dom.$('poseNormalize').addEventListener('change', (event) => {
      SkinTool.model.state.normalizeWeights = event.target.checked;
      SkinTool.deformer.update();
      SkinTool.anchors.sync();
    });
  }

  /** Ползунки и числа строятся один раз — иначе их сбрасывало бы при перерисовке. */
  function buildRotationControls() {
    const container = SkinTool.dom.$('poseRotation');
    const quick = SkinTool.dom.$('poseQuick');
    container.innerHTML = '';
    quick.innerHTML = '';

    AXES.forEach((axis, axisIndex) => {
      const row = document.createElement('div');
      row.className = 'pose-axis';

      const name = document.createElement('span');
      name.className = 'pose-axis-name';
      name.textContent = axis.label;
      name.style.color = axis.color;
      row.appendChild(name);

      const slider = document.createElement('input');
      slider.type = 'range';
      slider.id = 'poseSlider' + axis.key;
      slider.min = String(MIN_ANGLE);
      slider.max = String(MAX_ANGLE);
      slider.step = '1';
      slider.addEventListener('input', () => applyRotation(axisIndex, Number(slider.value)));
      row.appendChild(slider);

      const number = document.createElement('input');
      number.type = 'number';
      number.id = 'poseNumber' + axis.key;
      number.min = String(MIN_ANGLE);
      number.max = String(MAX_ANGLE);
      number.step = '1';
      number.addEventListener('input', () => {
        const value = clampAngle(Number(number.value));
        if (!isNaN(value)) applyRotation(axisIndex, value);
      });
      row.appendChild(number);

      container.appendChild(row);

      [-90, 90, 0].forEach((delta) => {
        const button = document.createElement('button');
        const value = delta === 0 ? '0°' : (delta > 0 ? '+90°' : '−90°');
        button.textContent = axis.label + ' ' + value;
        button.title = delta === 0
          ? 'Обнулить поворот по оси ' + axis.label
          : 'Повернуть на ' + delta + '° по оси ' + axis.label;
        button.addEventListener('click', () => {
          const current = getRotation();
          const next = current.slice();
          next[axisIndex] = delta === 0 ? 0 : clampAngle(current[axisIndex] + delta);
          applyRotation(axisIndex, next[axisIndex]);
        });
        quick.appendChild(button);
      });
    });
  }

  function buildBoneSelect() {
    SkinTool.dom.$('poseBone').addEventListener('change', (event) => {
      const id = parseInt(event.target.value);
      if (!isNaN(id)) SkinTool.bones.selectBone(id);
    });
  }

  function buildWeightHeader() {
    const normalize = SkinTool.dom.$('poseNormalize');
    normalize.checked = SkinTool.model.state.normalizeWeights;
  }

  function clampAngle(value) {
    if (isNaN(value)) return 0;
    // Поворот за ±180° сворачиваем в диапазон — иначе ползунок «уедет».
    let angle = value;
    while (angle > MAX_ANGLE) angle -= 360;
    while (angle < MIN_ANGLE) angle += 360;
    return Math.round(angle);
  }

  /** @param {number[]} rotation */
  function getRotation() {
    const bone = SkinTool.model.getSelectedBone();
    if (!bone) return [0, 0, 0];
    return SkinTool.deformer.getPoseRotation(bone.id);
  }

  /**
   * Записывает пробный поворот и обновляет сцену.
   *
   * На вкладке «Анимация» поворот во вьюпорте сразу уходит в кадр на
   * текущем времени — иначе пришлось бы нажимать «+ Кадр» после каждого
   * движения кольца.
   *
   * @param {number} axisIndex
   * @param {number} value
   */
  function applyRotation(axisIndex, value) {
    const bone = SkinTool.model.getSelectedBone();
    if (!bone) return;

    const rotation = getRotation().slice();
    rotation[axisIndex] = value;

    SkinTool.deformer.setPoseRotation(bone.id, rotation);
    SkinTool.anchors.sync();

    SkinTool.animationUI.syncRotationToKey(bone.id);

    syncRotationInputs(rotation);
    updatePoseReadout(rotation);
    updatePoseState();
  }

  /** Значения в ползунках и полях ввода — из текущей позы. */
  function syncRotationInputs(rotation) {
    AXES.forEach((axis, axisIndex) => {
      const slider = SkinTool.dom.$('poseSlider' + axis.key);
      const number = SkinTool.dom.$('poseNumber' + axis.key);
      const value = String(rotation[axisIndex]);

      if (document.activeElement !== slider) slider.value = value;
      if (document.activeElement !== number) number.value = value;
    });
  }

  /** Правая панель: значения позы выбранной кости. */
  function updatePoseReadout(rotation) {
    const container = SkinTool.dom.$('poseReadout');
    const bone = SkinTool.model.getSelectedBone();

    if (!bone) {
      container.innerHTML = '<div class="placeholder">Выберите кость</div>';
      return;
    }

    container.innerHTML = AXES.map((axis, axisIndex) =>
      `<div class="pose-readout-row">
        <span style="color:${axis.color}">${axis.label}</span>
        <b>${format(rotation[axisIndex])}°</b>
      </div>`
    ).join('') +
      `<div class="hint">Записано в кость: ${bone.rotation.map((v) => format(v) + '°').join(', ')}</div>`;
  }

  /** Статус пробной позы и кнопки записи/сброса. */
  function updatePoseState() {
    const container = SkinTool.dom.$('poseState');
    const bone = SkinTool.model.getSelectedBone();

    const applyBtn = SkinTool.dom.$('btnPoseApply');
    const discardBtn = SkinTool.dom.$('btnPoseDiscard');
    const posedCount = Object.keys(SkinTool.model.state.pose).length;

    if (!bone || !SkinTool.deformer.isPosed(bone.id)) {
      applyBtn.disabled = true;
      discardBtn.disabled = true;
    } else {
      applyBtn.disabled = false;
      discardBtn.disabled = false;
    }

    SkinTool.dom.$('btnPoseDiscardAll').disabled = posedCount === 0;

    if (posedCount === 0) {
      container.textContent = 'Пробной позы нет — модель в bind-посе.';
    } else {
      container.textContent = `Пробная поза: ${posedCount} костей. Экспорт её не видит.`;
    }
  }

  /** Список костей в select — с отступами по уровню дерева. */
  function updateBoneSelect() {
    const select = SkinTool.dom.$('poseBone');
    const { state, getChildBones } = SkinTool.model;

    const signature = JSON.stringify([
      state.bones.map((bone) => [bone.id, bone.name, bone.parentId]),
      state.selectedBoneId
    ]);
    if (signature === boneListSignature) return;
    boneListSignature = signature;

    select.innerHTML = '';

    if (state.bones.length === 0) {
      const option = document.createElement('option');
      option.value = '';
      option.textContent = '— костей нет —';
      select.appendChild(option);
      return;
    }

    const fragment = document.createDocumentFragment();

    function fill(parentId, depth) {
      getChildBones(parentId).forEach((bone) => {
        const option = document.createElement('option');
        option.value = String(bone.id);
        option.textContent = '— '.repeat(depth) + bone.name;
        if (bone.id === state.selectedBoneId) option.selected = true;
        fragment.appendChild(option);

        fill(bone.id, depth + 1);
      });
    }

    fill(null, 0);
    select.appendChild(fragment);
  }

  /** Правая панель: веса вершин выбранной кости. */
  function updateWeights() {
    const container = SkinTool.dom.$('poseWeights');
    const summary = SkinTool.dom.$('poseWeightsSummary');
    const bone = SkinTool.model.getSelectedBone();

    if (!bone) {
      container.innerHTML = '<div class="placeholder">Выберите кость</div>';
      summary.textContent = '';
      return;
    }

    const influence = SkinTool.deformer.getBoneInfluence(bone.id);
    summary.textContent =
      `вершин: ${influence.count} из ${influence.vertexCount} · сумма весов: ${format(influence.weightSum)}` +
      (influence.shared > 0 ? ` · общих с другими костями: ${influence.shared}` : '');

    if (bone.indices.length === 0) {
      container.innerHTML = '<div class="placeholder">У кости нет вершин</div>';
      return;
    }

    const { getWeight, buildVertexBoneMap } = SkinTool.model;
    const vertexMap = buildVertexBoneMap();
    const fragment = document.createDocumentFragment();

    bone.indices.forEach((vertexIndex, slot) => {
      const row = document.createElement('div');
      row.className = 'weight-row';

      const label = document.createElement('span');
      const shared = (vertexMap.get(vertexIndex) || []).length > 1;
      label.innerHTML = `#${vertexIndex}${shared ? ' <span class="weight-shared" title="вершина закреплена за несколькими костями">×N</span>' : ''}`;
      row.appendChild(label);

      const input = document.createElement('input');
      input.type = 'number';
      input.step = '0.05';
      input.min = '0';
      input.value = String(getWeight(bone, slot));
      input.addEventListener('change', () => {
        const weight = Number(input.value);
        bone.weights[slot] = isNaN(weight) ? 1 : Math.max(0, weight);
        SkinTool.deformer.rebuild();
        SkinTool.anchors.sync();
        updateWeights();
      });
      row.appendChild(input);

      fragment.appendChild(row);
    });

    container.innerHTML = '';
    container.appendChild(fragment);
  }

  /** Полное обновление вкладки — дёшево, если ничего не изменилось. */
  function updateTab() {
    if (SkinTool.model.state.currentTab !== 'pose') return;

    updateBoneSelect();
    syncRotationInputs(getRotation());
    updatePoseReadout(getRotation());
    updatePoseState();
    updateWeights();
  }

  /** Лёгкое обновление — вызывается при смене выбранной кости. */
  function refresh() {
    if (SkinTool.model.state.currentTab !== 'pose') return;

    updateBoneSelect();
    syncRotationInputs(getRotation());
    updatePoseReadout(getRotation());
    updatePoseState();
    updateWeights();
  }

  function format(value) {
    return Number.isInteger(value) ? String(value) : String(Number(value.toFixed(2)));
  }

  SkinTool.pose = { bind, updateTab, refresh, applyRotation, clampAngle };
})(window.SkinTool);
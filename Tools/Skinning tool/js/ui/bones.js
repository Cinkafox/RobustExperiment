/**
 * Работа с костями: создание, удаление, выбор, дерево в панели,
 * свойства выбранной кости и ручной ввод индексов вершин.
 */
(function (SkinTool) {
  'use strict';

  function bind() {
    SkinTool.dom.$('btnAddBone').addEventListener('click', addBone);
    SkinTool.dom.$('btnAddRoot').addEventListener('click', addRootBone);
    SkinTool.dom.$('btnAddManual').addEventListener('click', addManualIndices);
    SkinTool.dom.$('btnClearIndices').addEventListener('click', clearCurrentBoneIndices);
  }

  /** Перерисовывает всё, что зависит от состава скелета. */
  function refreshAll() {
    renderBonesList();
    renderBoneProps();
    renderIndicesList();
    SkinTool.colors.updatePointColors();
    SkinTool.deformer.rebuild();
    SkinTool.anchors.sync();
    SkinTool.skinning.updateTab();
    SkinTool.pose.updateTab();
  }

  // ---------- Создание и удаление ----------

  /** Новая кость без родителя. */
  function addRootBone() {
    const { state } = SkinTool.model;

    const bone = {
      id: state.nextBoneId++,
      name: 'bone_' + state.bones.length,
      position: [0, 0, 0],
      rotation: [0, 0, 0],
      indices: [],
      weights: [],
      parentId: null
    };

    state.bones.push(bone);
    state.selectedBoneId = bone.id;
    refreshAll();
  }

  /** Новая кость — ребёнок выбранной. */
  function addChildBone() {
    const { state } = SkinTool.model;
    const parent = SkinTool.model.getSelectedBone();

    if (!parent) {
      alert('Сначала выберите родительскую кость');
      return;
    }

    const bone = {
      id: state.nextBoneId++,
      name: parent.name + '_child',
      position: [0, 0, 0],
      rotation: [0, 0, 0],
      indices: [],
      weights: [],
      parentId: parent.id
    };

    state.bones.push(bone);
    state.selectedBoneId = bone.id;
    refreshAll();
  }

  /** Кость к выбранной, иначе — корневая. */
  function addBone() {
    if (SkinTool.model.state.selectedBoneId) addChildBone();
    else addRootBone();
  }

  /** @param {number} id */
  function selectBone(id) {
    SkinTool.model.state.selectedBoneId = id;
    refreshAll();
  }

  /** Удаляет кость вместе со всеми потомками. @param {number} id */
  function deleteBone(id) {
    if (!confirm('Удалить кость и всех её потомков?')) return;

    const { state, getChildBones } = SkinTool.model;

    const toDelete = new Set();
    (function collect(parentId) {
      toDelete.add(parentId);
      getChildBones(parentId).forEach((bone) => collect(bone.id));
    })(id);

    state.bones = state.bones.filter((bone) => !toDelete.has(bone.id));
    if (toDelete.has(state.selectedBoneId)) state.selectedBoneId = null;

    refreshAll();
  }

  // ---------- Дерево костей ----------

  function renderBonesList() {
    const { state, getChildBones, getBoneColorCss } = SkinTool.model;
    const container = SkinTool.dom.$('bonesList');
    container.innerHTML = '';

    function renderTree(parentEl, parentId, depth) {
      getChildBones(parentId).forEach((bone) => {
        const wrap = document.createElement('div');
        if (depth > 0) wrap.className = 'child-indent';

        const item = document.createElement('div');
        item.className = 'bone-item' + (bone.id === state.selectedBoneId ? ' selected' : '');

        const colorHex = getBoneColorCss(bone.id);

        const content = document.createElement('div');
        content.className = 'bone-content';
        content.innerHTML = `
          <div class="bone-name">
            <span class="bone-color-dot" style="background:${colorHex}"></span>
            ${'—'.repeat(depth)} ${bone.name}
          </div>
          <div class="bone-info">pos: [${bone.position.map((v) => v.toFixed(2)).join(', ')}] · rot: [${bone.rotation.join(', ')}] · idx: ${bone.indices.length}</div>
        `;
        content.addEventListener('click', (event) => {
          if (event.target.tagName === 'BUTTON') return;
          selectBone(bone.id);
        });

        const deleteBtn = document.createElement('button');
        deleteBtn.textContent = '✕';
        deleteBtn.className = 'delete-btn';
        deleteBtn.addEventListener('click', (event) => {
          event.stopPropagation();
          deleteBone(bone.id);
        });

        item.appendChild(content);
        item.appendChild(deleteBtn);
        wrap.appendChild(item);
        parentEl.appendChild(wrap);

        renderTree(wrap, bone.id, depth + 1);
      });
    }

    renderTree(container, null, 0);

    if (state.bones.length === 0) {
      container.innerHTML = '<div class="placeholder-sm">Нет костей. Добавьте первую.</div>';
    }
  }

  // ---------- Свойства кости ----------

  function createLabel(text) {
    const label = document.createElement('label');
    label.textContent = text;
    return label;
  }

  /**
   * Поле числа, применяющееся к выбранной кости сразу при вводе.
   * @param {HTMLInputElement} input
   * @param {'position'|'rotation'} propPath
   * @param {number} componentIndex
   */
  function bindAutoApplyInput(input, propPath, componentIndex) {
    const apply = () => {
      const bone = SkinTool.model.getSelectedBone();
      if (!bone) return;

      const value = parseFloat(input.value);
      if (isNaN(value)) return;

      bone[propPath][componentIndex] = value;
      SkinTool.deformer.rebuild();
      SkinTool.anchors.sync();
      renderBonesList();
      SkinTool.pose.updateTab();
    };

    input.addEventListener('change', apply);
    input.addEventListener('keydown', (event) => {
      if (event.key !== 'Enter') return;
      event.preventDefault();
      apply();
      input.blur();
    });
  }

  /** @param {HTMLSelectElement} select */
  function bindAutoApplyParent(select) {
    select.addEventListener('change', () => {
      const bone = SkinTool.model.getSelectedBone();
      if (!bone) return;

      bone.parentId = select.value === '' ? null : parseInt(select.value);
      renderBonesList();
      SkinTool.anchors.sync();
      SkinTool.dom.setStatus('Родитель изменён');
    });
  }

  function renderBoneProps() {
    const { state } = SkinTool.model;
    const container = SkinTool.dom.$('boneProps');
    const bone = SkinTool.model.getSelectedBone();

    container.innerHTML = '';
    if (!bone) {
      container.innerHTML = '<div class="placeholder">Выберите кость в списке</div>';
      return;
    }

    container.appendChild(createLabel('Имя'));
    const nameInput = document.createElement('input');
    nameInput.type = 'text';
    nameInput.value = bone.name;
    nameInput.addEventListener('input', (event) => {
      bone.name = event.target.value;
      renderBonesList();
    });
    container.appendChild(nameInput);

    container.appendChild(createLabel('Родитель'));
    const parentSelect = document.createElement('select');
    parentSelect.id = 'propParent';

    const rootOption = document.createElement('option');
    rootOption.value = '';
    rootOption.textContent = '— корень —';
    if (bone.parentId === null) rootOption.selected = true;
    parentSelect.appendChild(rootOption);

    state.bones.filter((other) => other.id !== bone.id).forEach((other) => {
      const option = document.createElement('option');
      option.value = other.id;
      option.textContent = other.name;
      if (other.id === bone.parentId) option.selected = true;
      parentSelect.appendChild(option);
    });

    bindAutoApplyParent(parentSelect);
    container.appendChild(parentSelect);

    container.appendChild(createLabel('Position (x,y,z)'));
    const positionRow = document.createElement('div');
    positionRow.className = 'row';
    ['propPx', 'propPy', 'propPz'].forEach((id, componentIndex) => {
      const input = document.createElement('input');
      input.type = 'number';
      input.step = '0.01';
      input.id = id;
      input.value = bone.position[componentIndex].toFixed(3);
      bindAutoApplyInput(input, 'position', componentIndex);
      positionRow.appendChild(input);
    });
    container.appendChild(positionRow);

    container.appendChild(createLabel('Rotation (x,y,z) в градусах'));
    const rotationRow = document.createElement('div');
    rotationRow.className = 'row';
    ['propRx', 'propRy', 'propRz'].forEach((id, componentIndex) => {
      const input = document.createElement('input');
      input.type = 'number';
      input.step = '1';
      input.id = id;
      input.value = bone.rotation[componentIndex];
      bindAutoApplyInput(input, 'rotation', componentIndex);
      rotationRow.appendChild(input);
    });
    container.appendChild(rotationRow);

    const buttonRow = document.createElement('div');
    buttonRow.className = 'btn-row';

    const applyBtn = document.createElement('button');
    applyBtn.id = 'btnApplyProps';
    applyBtn.textContent = 'Применить координаты';
    applyBtn.addEventListener('click', applyBoneProps);
    buttonRow.appendChild(applyBtn);

    const addChildBtn = document.createElement('button');
    addChildBtn.id = 'btnAddChild';
    addChildBtn.textContent = '+ Дочерняя кость';
    addChildBtn.addEventListener('click', addChildBone);
    buttonRow.appendChild(addChildBtn);

    container.appendChild(buttonRow);
  }

  /** Обновляет поля координат, не мешая пользователю печатать. */
  function updateBonePropsValues() {
    const bone = SkinTool.model.getSelectedBone();
    if (!bone) return;

    ['propPx', 'propPy', 'propPz'].forEach((id, componentIndex) => {
      const el = document.getElementById(id);
      if (el && document.activeElement !== el) {
        el.value = bone.position[componentIndex].toFixed(3);
      }
    });

    ['propRx', 'propRy', 'propRz'].forEach((id, componentIndex) => {
      const el = document.getElementById(id);
      if (el && document.activeElement !== el) {
        el.value = bone.rotation[componentIndex];
      }
    });

    const parentEl = document.getElementById('propParent');
    if (parentEl && document.activeElement !== parentEl) {
      parentEl.value = bone.parentId === null ? '' : bone.parentId;
    }
  }

  function applyBoneProps() {
    const bone = SkinTool.model.getSelectedBone();
    if (!bone) return;

    const parentValue = document.getElementById('propParent').value;
    bone.parentId = parentValue === '' ? null : parseInt(parentValue);

    ['propPx', 'propPy', 'propPz'].forEach((id, componentIndex) => {
      bone.position[componentIndex] = parseFloat(document.getElementById(id).value) || 0;
    });

    ['propRx', 'propRy', 'propRz'].forEach((id, componentIndex) => {
      bone.rotation[componentIndex] = parseFloat(document.getElementById(id).value) || 0;
    });

    refreshAll();
    SkinTool.dom.setStatus('Координаты и иерархия применены');
  }

  // ---------- Индексы вершин ----------

  function renderIndicesList() {
    const container = SkinTool.dom.$('indicesList');
    const countEl = SkinTool.dom.$('indicesCount');
    const bone = SkinTool.model.getSelectedBone();

    if (!bone || bone.indices.length === 0) {
      container.innerHTML = '—';
      countEl.textContent = 'Всего: 0';
      return;
    }

    const sorted = [...bone.indices].sort((a, b) => a - b);
    container.innerHTML = sorted.map((index) => `<span class="index-tag">${index}</span>`).join('');
    countEl.textContent = `Всего: ${sorted.length}`;
  }

  /**
   * Разбирает строку вида "222, 224, 230-241" и добавляет индексы в кость.
   */
  function addManualIndices() {
    const bone = SkinTool.model.getSelectedBone();
    if (!bone) {
      alert('Выберите кость');
      return;
    }

    const text = SkinTool.dom.$('manualIndices').value;
    let added = 0;

    text.split(/[,\s]+/).filter(Boolean).forEach((part) => {
      if (part.includes('-')) {
        const [from, to] = part.split('-').map((value) => parseInt(value.trim()));
        if (isNaN(from) || isNaN(to)) return;
        for (let index = Math.min(from, to); index <= Math.max(from, to); index++) {
          if (!bone.indices.includes(index)) {
            bone.indices.push(index);
            bone.weights.push(1);
            added++;
          }
        }
      } else {
        const index = parseInt(part);
        if (!isNaN(index) && !bone.indices.includes(index)) {
          bone.indices.push(index);
          bone.weights.push(1);
          added++;
        }
      }
    });

    SkinTool.dom.$('manualIndices').value = '';
    renderBonesList();
    renderIndicesList();
    SkinTool.colors.updatePointColors();
    SkinTool.deformer.rebuild();
    SkinTool.skinning.updateTab();
    SkinTool.pose.updateTab();
    SkinTool.dom.setStatus(`Добавлено ${added} индексов в "${bone.name}"`);
  }

  function clearCurrentBoneIndices() {
    const bone = SkinTool.model.getSelectedBone();
    if (!bone) return;

    bone.indices = [];
    bone.weights = [];
    renderBonesList();
    renderIndicesList();
    SkinTool.colors.updatePointColors();
    SkinTool.deformer.rebuild();
    SkinTool.skinning.updateTab();
    SkinTool.pose.updateTab();
  }

  SkinTool.bones = {
    bind,
    refreshAll,
    addBone,
    addRootBone,
    addChildBone,
    selectBone,
    deleteBone,
    renderBonesList,
    renderBoneProps,
    updateBonePropsValues,
    applyBoneProps,
    renderIndicesList,
    addManualIndices,
    clearCurrentBoneIndices
  };
})(window.SkinTool);
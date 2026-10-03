/**
 * Работа с мышью во вьюпорте: режимы назначения индексов
 * (клик по вершине / рамочное выделение), перетаскивание якорей костей,
 * вращение кости кольцами позинга и управление режимами.
 */
(function (SkinTool) {
  'use strict';

  const CLICK_THRESHOLD_PX = 5;
  const BOX_TOLERANCE_PX = 4;
  const POINT_PICK_THRESHOLD = 0.05;

  let raycaster = null;
  let mouse = null;
  let selectionBox = null;
  let isDragging = false;
  const dragStart = { x: 0, y: 0 };

  /** Угол позы на момент захвата кольца — база для относительного поворота. */
  let ringDragBaseAngle = 0;

  function bind() {
    const THREE = SkinTool.THREE;

    raycaster = new THREE.Raycaster();
    raycaster.params.Points = { threshold: POINT_PICK_THRESHOLD };
    mouse = new THREE.Vector2();
    selectionBox = SkinTool.dom.$('selectionBox');

    const canvas = SkinTool.scene.getCanvas();
    canvas.addEventListener('mousedown', onMouseDown);
    canvas.addEventListener('mousemove', onMouseMove);
    canvas.addEventListener('mouseup', onMouseUp);

    SkinTool.dom.$('modeNone').addEventListener('click', () => setSelectionMode('none'));
    SkinTool.dom.$('modeAdd').addEventListener('click', () => setSelectionMode('add'));
    SkinTool.dom.$('modeRemove').addEventListener('click', () => setSelectionMode('remove'));
    SkinTool.dom.$('btnBoxSelect').addEventListener('click', toggleBoxSelect);
    SkinTool.dom.$('btnResetCamera').addEventListener('click', () => SkinTool.scene.resetCamera());
  }

  /** Переводит координаты события в NDC-координаты мыши. */
  function updateMouse(event) {
    const rect = SkinTool.scene.getCanvas().getBoundingClientRect();
    mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
    mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;
  }

  /** Координаты события относительно вьюпорта (а не окна). */
  function toViewportCoords(event) {
    const rect = SkinTool.scene.getViewport().getBoundingClientRect();
    return {
      left: event.clientX - rect.left,
      top: event.clientY - rect.top
    };
  }

  function onMouseDown(event) {
    if (event.button !== 0) return;

    const { state } = SkinTool.model;
    updateMouse(event);

    // Кольца позинга ловятся раньше якорей и вершин: в режиме позинга
    // это единственное, что должно реагировать на клик по кости.
    if (SkinTool.skeleton.isVisible()) {
      const axisIndex = SkinTool.skeleton.pickRing(event.clientX, event.clientY);

      if (axisIndex >= 0) {
        const bone = SkinTool.model.getSelectedBone();
        const rotation = bone ? SkinTool.deformer.getPoseRotation(bone.id) : [0, 0, 0];

        // Запоминаем угол оси на момент захвата: дальше прибавляем к нему
        // поворот курсора, иначе взятое «за спиной» у дуги кольцо дёрнуло
        // бы кость на свой угол.
        ringDragBaseAngle = rotation[axisIndex] || 0;
        SkinTool.skeleton.beginRingDrag(axisIndex, event.clientX, event.clientY);
        SkinTool.scene.getControls().enabled = false;
        setCanvasCursor('grabbing');
        SkinTool.dom.setStatus(
          'Поворот по оси ' + SkinTool.skeleton.getAxisLabel(axisIndex) + ' — тяните кольцо'
        );
        return;
      }
    }

    // Затем пытаемся схватить якорь кости — он лежит поверх точек.
    if (state.showAnchors) {
      raycaster.setFromCamera(mouse, SkinTool.scene.getCamera());
      const anchorHits = raycaster.intersectObjects(SkinTool.anchors.getVisibleAnchorMeshes());

      if (anchorHits.length > 0) {
        const boneId = anchorHits[0].object.parent.userData.boneId;
        if (SkinTool.model.getBone(boneId)) {
          SkinTool.bones.selectBone(boneId);
          SkinTool.anchors.beginDrag(boneId, raycaster);
          SkinTool.scene.getControls().enabled = false;
          return;
        }
      }
    }

    if (state.selectionMode === 'none' || !state.selectedBoneId) return;

    isDragging = true;
    dragStart.x = event.clientX;
    dragStart.y = event.clientY;

    if (state.isBoxSelectMode) {
      SkinTool.scene.getControls().enabled = false;
    }

    const start = toViewportCoords(dragStart);
    selectionBox.style.left = start.left + 'px';
    selectionBox.style.top = start.top + 'px';
    selectionBox.style.width = '0px';
    selectionBox.style.height = '0px';
    selectionBox.style.display = 'block';
  }

  function onMouseMove(event) {
    // Перетаскивание якоря имеет приоритет над всем остальным.
    if (SkinTool.anchors.getDraggingBoneId() !== null) {
      updateMouse(event);
      raycaster.setFromCamera(mouse, SkinTool.scene.getCamera());

      const newPosition = SkinTool.anchors.updateDrag(raycaster);
      const bone = SkinTool.model.getBone(SkinTool.anchors.getDraggingBoneId());
      if (newPosition && bone) {
        bone.position = [newPosition.x, newPosition.y, newPosition.z];
        SkinTool.anchors.sync();
        SkinTool.bones.updateBonePropsValues();
        SkinTool.bones.renderBonesList();
      }
      return;
    }

    // Вращение кости кольцом: берём поворот курсора относительно точки
    // захвата и сразу пишем в пробную позу — гизмо обновится на кадре.
    const draggingAxis = SkinTool.skeleton.getDraggingAxis();
    if (draggingAxis >= 0) {
      const delta = SkinTool.skeleton.getRingDragDelta(event.clientX, event.clientY);
      if (delta !== null) {
        const degrees = ringDragBaseAngle + delta;
        SkinTool.pose.applyRotation(draggingAxis, SkinTool.pose.clampAngle(Math.round(degrees)));
      }
      return;
    }

    // Курсор над кольцом — видно, что его можно тянуть.
    if (SkinTool.skeleton.isVisible()) {
      const overRing = SkinTool.skeleton.pickRing(event.clientX, event.clientY) >= 0;
      setCanvasCursor(overRing ? 'grab' : '');
    }

    // В режиме скиннинга наведение показывает детали вершины.
    if (SkinTool.model.state.currentTab === 'skinning' && SkinTool.scene.getPoints()) {
      updateMouse(event);
      raycaster.setFromCamera(mouse, SkinTool.scene.getCamera());
      const hits = raycaster.intersectObject(SkinTool.scene.getPoints());
      if (hits.length > 0) {
        SkinTool.skinning.showVertexDetails(hits[0].index);
      }
    }

    if (!isDragging) return;

    const start = toViewportCoords(dragStart);
    const end = toViewportCoords(event);

    selectionBox.style.left = Math.min(start.left, end.left) + 'px';
    selectionBox.style.top = Math.min(start.top, end.top) + 'px';
    selectionBox.style.width = Math.abs(end.left - start.left) + 'px';
    selectionBox.style.height = Math.abs(end.top - start.top) + 'px';
  }

  function onMouseUp(event) {
    if (SkinTool.anchors.getDraggingBoneId() !== null) {
      SkinTool.anchors.endDrag();
      SkinTool.scene.getControls().enabled = true;
      return;
    }

    if (SkinTool.skeleton.getDraggingAxis() >= 0) {
      const bone = SkinTool.model.getSelectedBone();
      SkinTool.skeleton.endRingDrag();
      ringDragBaseAngle = 0;
      SkinTool.scene.getControls().enabled = true;
      setCanvasCursor('');

      const rotation = bone ? SkinTool.deformer.getPoseRotation(bone.id) : [0, 0, 0];
      // На «Анимации» поворот уже лёг в кадр на текущем времени — говорим
      // об этом, иначе статус остался бы от позинга и путал.
      const prefix = SkinTool.model.state.currentTab === 'animation'
        ? `Кадр на ${SkinTool.animationUI.getTime().toFixed(2)} с`
        : 'Пробный поворот';

      SkinTool.dom.setStatus(
        bone
          ? `${prefix} "${bone.name}": ${rotation.map((v) => v + '°').join(', ')}`
          : 'Поворот сброшен'
      );
      return;
    }

    if (!isDragging) return;
    isDragging = false;
    selectionBox.style.display = 'none';

    const { state } = SkinTool.model;

    if (state.isBoxSelectMode) {
      SkinTool.scene.getControls().enabled = true;
    }

    const bone = SkinTool.model.getSelectedBone();
    if (!bone) return;

    const distance = Math.hypot(event.clientX - dragStart.x, event.clientY - dragStart.y);
    let changed = false;

    if (distance < CLICK_THRESHOLD_PX || !state.isBoxSelectMode) {
      changed = applyToPickedVertex(event, bone);
    } else {
      changed = applyToBox(event, bone);
    }

    if (!changed) return;

    SkinTool.colors.updatePointColors();
    SkinTool.bones.renderBonesList();
    SkinTool.bones.renderIndicesList();
    SkinTool.deformer.rebuild();
    SkinTool.skinning.updateTab();
    SkinTool.pose.updateTab();
    SkinTool.dom.setStatus(`Обновлено выделение для "${bone.name}"`);
  }

  /** Клик по вершине: добавляет или убирает её из кости. */
  function applyToPickedVertex(event, bone) {
    const { state } = SkinTool.model;

    updateMouse(event);
    raycaster.setFromCamera(mouse, SkinTool.scene.getCamera());

    const hits = raycaster.intersectObject(SkinTool.scene.getPoints());
    if (hits.length === 0) return false;

    return toggleIndex(bone, hits[0].index, state.selectionMode);
  }

  /** Рамка: назначает индексы всем вершинам внутри прямоугольника. */
  function applyToBox(event, bone) {
    const { state } = SkinTool.model;
    const viewportRect = SkinTool.scene.getViewport().getBoundingClientRect();

    const boxLeft = Math.min(dragStart.x, event.clientX) - viewportRect.left;
    const boxTop = Math.min(dragStart.y, event.clientY) - viewportRect.top;
    const boxRight = Math.max(dragStart.x, event.clientX) - viewportRect.left;
    const boxBottom = Math.max(dragStart.y, event.clientY) - viewportRect.top;

    const positions = SkinTool.scene.getVertexPositions();
    const projected = new SkinTool.THREE.Vector3();

    let changed = false;

    for (let i = 0; i < state.vertices.length; i++) {
      projected.set(positions[i * 3], positions[i * 3 + 1], positions[i * 3 + 2]);
      projected.project(SkinTool.scene.getCamera());

      if (projected.z >= 1) continue;

      const screenX = (projected.x * 0.5 + 0.5) * viewportRect.width;
      const screenY = (-projected.y * 0.5 + 0.5) * viewportRect.height;

      const inside =
        screenX >= boxLeft - BOX_TOLERANCE_PX && screenX <= boxRight + BOX_TOLERANCE_PX &&
        screenY >= boxTop - BOX_TOLERANCE_PX && screenY <= boxBottom + BOX_TOLERANCE_PX;

      if (inside && toggleIndex(bone, i, state.selectionMode)) {
        changed = true;
      }
    }

    return changed;
  }

  /**
   * @param {object} bone
   * @param {number} index — индекс вершины
   * @param {string} mode — 'add' | 'remove'
   * @returns {boolean} изменилось ли назначение
   */
  function toggleIndex(bone, index, mode) {
    if (mode === 'add') {
      if (bone.indices.includes(index)) return false;
      bone.indices.push(index);
      bone.weights.push(1);
      return true;
    }

    const position = bone.indices.indexOf(index);
    if (position === -1) return false;

    bone.indices.splice(position, 1);
    bone.weights.splice(position, 1);
    return true;
  }

  /** Курсор вьюпорта: подсказывает, что кольцо или точка тянутся. */
  function setCanvasCursor(cursor) {
    const canvas = SkinTool.scene.getCanvas();
    if (canvas.style.cursor !== cursor) canvas.style.cursor = cursor;
  }

  /** @param {'none'|'add'|'remove'} mode */
  function setSelectionMode(mode) {
    SkinTool.model.state.selectionMode = mode;

    SkinTool.dom.$('modeNone').classList.toggle('active', mode === 'none');
    SkinTool.dom.$('modeAdd').classList.toggle('active', mode === 'add');
    SkinTool.dom.$('modeRemove').classList.toggle('active', mode === 'remove');

    const modeText = mode === 'none' ? 'выключен' : mode === 'add' ? 'добавление' : 'удаление';
    SkinTool.dom.setStatus(`Режим: ${modeText}`);
  }

  function toggleBoxSelect() {
    const state = SkinTool.model.state;
    state.isBoxSelectMode = !state.isBoxSelectMode;

    SkinTool.dom.$('btnBoxSelect').classList.toggle('active', state.isBoxSelectMode);
    SkinTool.dom.setStatus(state.isBoxSelectMode ? 'Режим рамки включен' : 'Режим рамки выключен');
  }

  SkinTool.selection = {
    bind,
    setSelectionMode,
    toggleBoxSelect
  };
})(window.SkinTool);
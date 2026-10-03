/**
 * Якоря костей: маркер позиции/ориентации кости во вьюпорте.
 *
 * Каждой кости соответствует группа (сфера + три оси). Якорь можно
 * перетаскивать мышью — для этого модуль держит плоскость перетаскивания
 * и смещение точки захвата.
 */
(function (SkinTool) {
  'use strict';

  const boneAnchors = new Map();

  let dragPlane = null;
  let dragOffset = null;
  let draggingBoneId = null;

  const AXIS_LENGTH = 0.1;
  const AXIS_COLORS = [0xff0000, 0x00ff00, 0x0000ff];
  const SPHERE_RADIUS = 0.03;

  function init() {
    dragPlane = new SkinTool.THREE.Plane();
    dragOffset = new SkinTool.THREE.Vector3();
    syncToggleButton();
  }

  function bind() {
    SkinTool.dom.$('btnToggleAnchors').addEventListener('click', toggle);
  }

  /**
   * @param {{id:number, position:number[], rotation:number[]}} bone
   * @returns {THREE.Group}
   */
  function createAnchor(bone) {
    const THREE = SkinTool.THREE;
    const group = new THREE.Group();

    const sphere = new THREE.Mesh(
      new THREE.SphereGeometry(SPHERE_RADIUS, 16, 16),
      new THREE.MeshBasicMaterial({ color: SkinTool.model.getBoneColor(bone.id) })
    );
    group.add(sphere);

    const axisDirections = [
      new THREE.Vector3(1, 0, 0),
      new THREE.Vector3(0, 1, 0),
      new THREE.Vector3(0, 0, 1)
    ];

    axisDirections.forEach((direction, axisIndex) => {
      const lineGeometry = new THREE.BufferGeometry().setFromPoints([
        new THREE.Vector3(0, 0, 0),
        direction.clone().multiplyScalar(AXIS_LENGTH)
      ]);
      const lineMaterial = new THREE.LineBasicMaterial({ color: AXIS_COLORS[axisIndex] });
      group.add(new THREE.Line(lineGeometry, lineMaterial));
    });

    group.userData.boneId = bone.id;
    updateAnchorTransform(bone, group);
    SkinTool.scene.add(group);
    return group;
  }

  /**
   * Переносит позицию/поворот кости в сцену и подсвечивает выбранную кость.
   *
   * Позиция и ориентация берутся из матрицы позы (SkinTool.deformer),
   * поэтому при проверке позы якоря двигаются вместе со скелетом,
   * а в спокойном состоянии совпадают с bone.position.
   *
   * @param {object} bone
   * @param {THREE.Group} anchor
   */
  function updateAnchorTransform(bone, anchor) {
    const matrix = SkinTool.deformer ? SkinTool.deformer.getBoneMatrix(bone.id) : null;

    if (matrix) {
      matrix.decompose(anchor.position, anchor.quaternion, anchor.scale);
    } else {
      anchor.position.set(bone.position[0], bone.position[1], bone.position[2]);
      anchor.rotation.set(
        SkinTool.THREE.MathUtils.degToRad(bone.rotation[0]),
        SkinTool.THREE.MathUtils.degToRad(bone.rotation[1]),
        SkinTool.THREE.MathUtils.degToRad(bone.rotation[2])
      );
    }

    const sphere = anchor.children[0];
    if (bone.id === SkinTool.model.state.selectedBoneId) {
      sphere.material.color.set(0xffffff);
      sphere.scale.set(1.5, 1.5, 1.5);
    } else {
      sphere.material.color.set(SkinTool.model.getBoneColor(bone.id));
      sphere.scale.set(1, 1, 1);
    }
  }

  /** Приводит набор якорей в соответствие со списком костей. */
  function sync() {
    const { state } = SkinTool.model;

    boneAnchors.forEach((anchor, boneId) => {
      if (!SkinTool.model.getBone(boneId)) {
        SkinTool.scene.remove(anchor);
        boneAnchors.delete(boneId);
      }
    });

    state.bones.forEach((bone) => {
      let anchor = boneAnchors.get(bone.id);
      if (!anchor) {
        anchor = createAnchor(bone);
        boneAnchors.set(bone.id, anchor);
      }
      updateAnchorTransform(bone, anchor);
      anchor.visible = state.showAnchors;
    });
  }

  function toggle() {
    SkinTool.model.state.showAnchors = !SkinTool.model.state.showAnchors;
    syncToggleButton();
    sync();
  }

  function syncToggleButton() {
    SkinTool.dom.$('btnToggleAnchors').classList.toggle('active', SkinTool.model.state.showAnchors);
  }

  /** Сферы видимых якорей — по ним идёт пикинг мышью. */
  function getVisibleAnchorMeshes() {
    const meshes = [];
    boneAnchors.forEach((anchor) => {
      if (anchor.visible) meshes.push(anchor.children[0]);
    });
    return meshes;
  }

  /**
   * Начинает перетаскивание якоря.
   * @param {number} boneId
   * @param {THREE.Raycaster} raycaster
   */
  function beginDrag(boneId, raycaster) {
    const THREE = SkinTool.THREE;
    const bone = SkinTool.model.getBone(boneId);
    if (!bone) return;

    draggingBoneId = boneId;

    const cameraDirection = new THREE.Vector3();
    SkinTool.scene.getCamera().getWorldDirection(cameraDirection);

    const anchorPosition = new THREE.Vector3();
    const anchor = boneAnchors.get(boneId);
    if (anchor) anchorPosition.copy(anchor.position);
    else anchorPosition.set(bone.position[0], bone.position[1], bone.position[2]);

    dragPlane.setFromNormalAndCoplanarPoint(cameraDirection, anchorPosition);

    const intersection = new THREE.Vector3();
    if (raycaster.ray.intersectPlane(dragPlane, intersection)) {
      dragOffset.copy(anchorPosition).sub(intersection);
    }
  }

  /**
   * Новая позиция якоря под текущим курсором.
   * @param {THREE.Raycaster} raycaster
   * @returns {THREE.Vector3|null}
   */
  function updateDrag(raycaster) {
    if (draggingBoneId === null) return null;

    const intersection = new SkinTool.THREE.Vector3();
    if (!raycaster.ray.intersectPlane(dragPlane, intersection)) return null;

    return intersection.add(dragOffset);
  }

  function endDrag() {
    draggingBoneId = null;
  }

  function getDraggingBoneId() {
    return draggingBoneId;
  }

  /**
   * Мировая позиция якоря кости в сцене.
   * Отличается от bone.position, когда кость повёрнута в пробной позе.
   * @param {number} boneId
   * @returns {{x:number, y:number, z:number}|null}
   */
  function getAnchorPosition(boneId) {
    const anchor = boneAnchors.get(boneId);
    if (!anchor) return null;

    return {
      x: anchor.position.x,
      y: anchor.position.y,
      z: anchor.position.z
    };
  }

  SkinTool.anchors = {
    init,
    bind,
    sync,
    toggle,
    getVisibleAnchorMeshes,
    getAnchorPosition,
    beginDrag,
    updateDrag,
    endDrag,
    getDraggingBoneId
  };
})(window.SkinTool);
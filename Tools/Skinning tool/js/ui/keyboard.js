/**
 * Точная подстройка положения якоря выбранной кости стрелками.
 * Шаг — вдоль осей камеры, чтобы двигать было интуитивно.
 *
 * На вкладке «Анимации» стрелки заняты таймлайном (перемотка), поэтому
 * здесь вкладку пропускаем.
 */
(function (SkinTool) {
  'use strict';

  const BASE_STEP = 0.1;
  const FAST_STEP_MULTIPLIER = 5;

  function bind() {
    window.addEventListener('keydown', onKeyDown);
  }

  function isTypingTarget(target) {
    const tag = target.tagName;
    return tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT';
  }

  /** @param {KeyboardEvent} event */
  function onKeyDown(event) {
    if (isTypingTarget(event.target)) return;
    if (SkinTool.model.state.currentTab === 'animation') return;

    const bone = SkinTool.model.getSelectedBone();
    if (!bone) return;

    const step = event.shiftKey ? BASE_STEP * FAST_STEP_MULTIPLIER : BASE_STEP;

    let right = 0;
    let up = 0;
    let forward = 0;

    switch (event.key) {
      case 'ArrowLeft': right = -step; break;
      case 'ArrowRight': right = step; break;
      case 'ArrowUp': up = step; break;
      case 'ArrowDown': up = -step; break;
      case 'PageUp': forward = step; break;
      case 'PageDown': forward = -step; break;
      default: return;
    }

    event.preventDefault();

    const camera = SkinTool.scene.getCamera();
    const cameraForward = new SkinTool.THREE.Vector3();
    const cameraRight = new SkinTool.THREE.Vector3();
    const cameraUp = new SkinTool.THREE.Vector3();

    camera.getWorldDirection(cameraForward);
    cameraRight.setFromMatrixColumn(camera.matrixWorld, 0);
    cameraUp.setFromMatrixColumn(camera.matrixWorld, 1);

    bone.position[0] += cameraRight.x * right + cameraUp.x * up + cameraForward.x * forward;
    bone.position[1] += cameraRight.y * right + cameraUp.y * up + cameraForward.y * forward;
    bone.position[2] += cameraRight.z * right + cameraUp.z * up + cameraForward.z * forward;

    SkinTool.deformer.rebuild();
    SkinTool.anchors.sync();
    SkinTool.bones.updateBonePropsValues();
    SkinTool.bones.renderBonesList();
    SkinTool.pose.updateTab();
    SkinTool.dom.setStatus(`Якорь "${bone.name}" смещён`);
  }

  SkinTool.keyboard = { bind };
})(window.SkinTool);
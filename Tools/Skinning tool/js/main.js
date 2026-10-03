/**
 * Точка входа: создаёт Three.js-сцену, подключает обработчики
 * всех модулей и выполняет первоначальную отрисовку панелей.
 */
(function (SkinTool) {
  'use strict';

  function bindModules() {
    SkinTool.tabs.bind();

    SkinTool.obj.bind();
    SkinTool.yamlImport.bind();
    SkinTool.yamlExport.bind();

    SkinTool.bones.bind();
    SkinTool.selection.bind();
    SkinTool.skinning.bind();
    SkinTool.pose.bind();
    SkinTool.anchors.bind();
    SkinTool.skeleton.bind();
    SkinTool.keyboard.bind();
  }

  function renderInitialState() {
    SkinTool.selection.setSelectionMode('none');
    SkinTool.bones.renderBonesList();
    SkinTool.bones.renderBoneProps();
    SkinTool.bones.renderIndicesList();
  }

  function init() {
    SkinTool.scene.init();
    SkinTool.anchors.init();
    SkinTool.skeleton.init();
    bindModules();
    renderInitialState();
  }

  SkinTool.onReady(init);
})(window.SkinTool);
/**
 * Загрузка OBJ-модели: файл -> вершины и треугольники -> сцена.
 */
(function (SkinTool) {
  'use strict';

  function bind() {
    SkinTool.dom.bindDropZone('objDrop', 'fileInput', readFile);
  }

  /** @param {File} file */
  function readFile(file) {
    const reader = new FileReader();
    reader.onload = (loadEvent) => loadFromText(loadEvent.target.result);
    reader.readAsText(file);
  }

  /**
   * Разбирает текст OBJ.
   * Грани-квады и многоугольники разбиваются на треугольники.
   *
   * @param {string} text
   * @returns {{verts: {x:number,y:number,z:number}[], indices: number[]}}
   */
  function parseOBJ(text) {
    const verts = [];
    const indices = [];

    text.split('\n').forEach((rawLine) => {
      const parts = rawLine.trim().split(/\s+/);
      if (parts[0] === 'v') {
        verts.push({ x: parseFloat(parts[1]), y: parseFloat(parts[2]), z: parseFloat(parts[3]) });
      } else if (parts[0] === 'f') {
        const face = [];
        for (let i = 1; i < parts.length; i++) {
          face.push(parseInt(parts[i].split('/')[0]) - 1);
        }
        for (let i = 1; i < face.length - 1; i++) {
          indices.push(face[0], face[i], face[i + 1]);
        }
      }
    });

    return { verts, indices };
  }

  /**
   * Разбирает OBJ и помещает модель в сцену.
   * @param {string} text
   */
  function loadFromText(text) {
    const { verts, indices } = parseOBJ(text);

    SkinTool.model.state.vertices = verts;
    SkinTool.scene.setMesh(verts, indices);
    SkinTool.scene.setPoints(verts);

    SkinTool.deformer.rebuild();
    SkinTool.anchors.sync();
    SkinTool.scene.resetCamera();
    SkinTool.colors.updatePointColors();
    SkinTool.pose.updateTab();
    SkinTool.dom.setStatus(`Загружено: ${verts.length} вершин, ${indices.length / 3} треугольников`);
  }

  SkinTool.obj = { bind, parseOBJ, loadFromText };
})(window.SkinTool);
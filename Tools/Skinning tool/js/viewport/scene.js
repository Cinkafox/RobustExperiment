/**
 * Three.js-сцена: рендерер, камера, освещение, сетка,
 * а также построение меша модели и облака вершин.
 *
 * Меш и облако вершин используют один и тот же буфер позиций
 * (skinMatrixes не копируют вершины по граням), поэтому деформация
 * скелета применяется к обоим объектам одной записью — updatePositions.
 *
 * Хранит геометрию модели в приватных переменных и отдаёт наружу
 * только нужные модулям куски (через геттеры).
 */
(function (SkinTool) {
  'use strict';

  let viewport = null;
  let scene = null;
  let camera = null;
  let renderer = null;
  let controls = null;

  let mesh = null;
  let points = null;
  let vertexPositions = null;
  let positionAttribute = null;
  let pointColors = null;

  /** Колбэки оверлеев: вызываются каждый кадр перед рендером. */
  const frameListeners = [];

  function init() {
    const THREE = SkinTool.THREE;

    viewport = SkinTool.dom.$('viewport');

    scene = new THREE.Scene();
    scene.background = new THREE.Color(0x1a1a2a);

    camera = new THREE.PerspectiveCamera(60, 1, 0.01, 1000);
    camera.position.set(2, 2, 3);

    renderer = new THREE.WebGLRenderer({ antialias: true });
    viewport.appendChild(renderer.domElement);

    controls = new SkinTool.OrbitControls(camera, renderer.domElement);
    controls.enableDamping = true;

    scene.add(new THREE.AmbientLight(0xffffff, 0.6));
    const dirLight = new THREE.DirectionalLight(0xffffff, 0.8);
    dirLight.position.set(5, 10, 5);
    scene.add(dirLight);

    scene.add(new THREE.GridHelper(10, 20, 0x444466, 0x2a2a3a));

    window.addEventListener('resize', resize);
    resize();
    animate();
  }

  function resize() {
    const width = viewport.clientWidth;
    const height = viewport.clientHeight;
    renderer.setSize(width, height);
    renderer.setPixelRatio(window.devicePixelRatio);
    camera.aspect = width / height;
    camera.updateProjectionMatrix();
  }

  function animate() {
    requestAnimationFrame(animate);
    controls.update();

    // Оверлеи читают состояние сами — им не нужно знать, кто изменил позу.
    for (let i = 0; i < frameListeners.length; i++) frameListeners[i]();

    renderer.render(scene, camera);
  }

  /**
   * Подписка на кадр — для оверлеев, которые перерисовываются каждый кадр.
   * @param {Function} listener
   */
  function addFrameListener(listener) {
    frameListeners.push(listener);
  }

  /** Наводит камеру на габариты загруженной модели. */
  function resetCamera() {
    const THREE = SkinTool.THREE;

    if (vertexPositions && vertexPositions.length > 0) {
      const box = new THREE.Box3();
      const vertex = new THREE.Vector3();
      for (let i = 0; i < vertexPositions.length; i += 3) {
        vertex.set(vertexPositions[i], vertexPositions[i + 1], vertexPositions[i + 2]);
        box.expandByPoint(vertex);
      }
      const center = new THREE.Vector3();
      box.getCenter(center);
      const size = new THREE.Vector3();
      box.getSize(size);
      const maxDim = Math.max(size.x, size.y, size.z);
      camera.position.set(center.x + maxDim, center.y + maxDim * 0.5, center.z + maxDim);
      controls.target.copy(center);
      controls.update();
    } else {
      camera.position.set(2, 2, 3);
      controls.target.set(0, 0, 0);
      controls.update();
    }
  }

  /** Полностью убирает текущую модель со сцены. */
  function clearModel() {
    if (mesh) {
      scene.remove(mesh);
      mesh.geometry.dispose();
      mesh.material.dispose();
      mesh = null;
    }
    if (points) {
      scene.remove(points);
      points.geometry.dispose();
      points.material.dispose();
      points = null;
    }
    vertexPositions = null;
    positionAttribute = null;
    pointColors = null;
  }

  /**
   * Создаёт общий буфер координат вершин и общий THREE-атрибут.
   * Один буфер на меш и облако — иначе деформацию пришлось бы
   * применять дважды и они бы разъехались.
   *
   * @param {number} count — количество вершин модели
   */
  function ensureVertexPositions(count) {
    if (vertexPositions && vertexPositions.length === count * 3) return;

    vertexPositions = new Float32Array(count * 3);
    positionAttribute = new THREE.BufferAttribute(vertexPositions, 3);

    for (let i = 0; i < count; i++) {
      vertexPositions[i * 3] = 0;
      vertexPositions[i * 3 + 1] = 0;
      vertexPositions[i * 3 + 2] = 0;
    }
  }

  /**
   * Строит полигональный меш модели.
   *
   * Геометрия индексированная: вершины не дублируются по граням,
   * их номера совпадают с номерами в state.vertices и в облаке точек.
   *
   * @param {{x:number,y:number,z:number}[]} verts
   * @param {number[]} faceIndices — развёрнутый список индексов треугольников
   */
  function setMesh(verts, faceIndices) {
    const THREE = SkinTool.THREE;

    if (mesh) {
      scene.remove(mesh);
      mesh.geometry.dispose();
      mesh.material.dispose();
    }

    ensureVertexPositions(verts.length);
    writeBindPositions(verts);

    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', positionAttribute);
    geometry.setIndex(faceIndices);
    geometry.computeVertexNormals();

    const material = new THREE.MeshStandardMaterial({
      color: 0x8888aa,
      side: THREE.DoubleSide,
      flatShading: true,
      transparent: true,
      opacity: 0.7
    });

    mesh = new THREE.Mesh(geometry, material);
    scene.add(mesh);
  }

  /** Кладёт исходные (bind) координаты вершин в общий буфер. */
  function writeBindPositions(verts) {
    for (let i = 0; i < verts.length; i++) {
      vertexPositions[i * 3] = verts[i].x;
      vertexPositions[i * 3 + 1] = verts[i].y;
      vertexPositions[i * 3 + 2] = verts[i].z;
    }
  }

  /**
   * Записывает новые координаты вершин (после деформации скелета)
   * и просит WebGL обновить буфер.
   *
   * @param {Float32Array} positions
   */
  function updatePositions(positions) {
    if (!vertexPositions || positions.length !== vertexPositions.length) return;

    vertexPositions.set(positions);

    // Атрибут общий у меша и облака точек, поэтому одного флага достаточно.
    positionAttribute.needsUpdate = true;

    if (mesh) mesh.geometry.computeVertexNormals();
  }

  /**
   * Строит облако вершин — по нему идёт пикинг, рамочное выделение и раскраска.
   * @param {{x:number,y:number,z:number}[]} verts
   */
  function setPoints(verts) {
    const THREE = SkinTool.THREE;

    if (points) {
      scene.remove(points);
      points.geometry.dispose();
      points.material.dispose();
    }

    ensureVertexPositions(verts.length);
    writeBindPositions(verts);

    pointColors = new Float32Array(verts.length * 3);
    for (let i = 0; i < verts.length; i++) {
      pointColors[i * 3] = 0.267;
      pointColors[i * 3 + 1] = 0.867;
      pointColors[i * 3 + 2] = 1.0;
    }

    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', positionAttribute);
    geometry.setAttribute('color', new THREE.BufferAttribute(pointColors, 3));

    const material = new THREE.PointsMaterial({
      vertexColors: true,
      size: 0.05,
      sizeAttenuation: true
    });

    points = new THREE.Points(geometry, material);
    scene.add(points);
  }

  function markColorsDirty() {
    if (points) {
      points.geometry.attributes.color.needsUpdate = true;
    }
  }

  /**
   * Кратковременно подсвечивает вершину жёлтым.
   * @param {number} index
   * @param {number} [durationMs]
   */
  function flashVertex(index, durationMs = 800) {
    if (!pointColors || !points) return;

    const previous = [
      pointColors[index * 3],
      pointColors[index * 3 + 1],
      pointColors[index * 3 + 2]
    ];

    pointColors[index * 3] = 1;
    pointColors[index * 3 + 1] = 1;
    pointColors[index * 3 + 2] = 0;
    markColorsDirty();

    setTimeout(() => {
      pointColors[index * 3] = previous[0];
      pointColors[index * 3 + 1] = previous[1];
      pointColors[index * 3 + 2] = previous[2];
      markColorsDirty();
    }, durationMs);
  }

  /** Переводит цель орбитальной камеры в точку мира. */
  function focusOnVertex(vertex) {
    controls.target.set(vertex.x, vertex.y, vertex.z);
    controls.update();
  }

  /**
   * Текущая (уже деформированная) позиция вершины.
   * @param {number} index
   * @returns {{x:number,y:number,z:number}|null}
   */
  function getVertexPosition(index) {
    const { vertices } = SkinTool.model.state;
    if (!vertices[index]) return null;
    if (!vertexPositions) return vertices[index];

    return {
      x: vertexPositions[index * 3],
      y: vertexPositions[index * 3 + 1],
      z: vertexPositions[index * 3 + 2]
    };
  }

  /** @param {THREE.Object3D} object */
  function add(object) {
    scene.add(object);
  }

  /** @param {THREE.Object3D} object */
  function remove(object) {
    scene.remove(object);
  }

  SkinTool.scene = {
    init,
    getScene: () => scene,
    getViewport: () => viewport,
    getCanvas: () => renderer.domElement,
    getCamera: () => camera,
    getControls: () => controls,
    getPoints: () => points,
    add,
    remove,
    getVertexPositions: () => vertexPositions,
    getPointColors: () => pointColors,
    getVertexPosition,
    addFrameListener,
    clearModel,
    setMesh,
    setPoints,
    updatePositions,
    markColorsDirty,
    flashVertex,
    focusOnVertex,
    resetCamera
  };
})(window.SkinTool);
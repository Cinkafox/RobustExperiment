/**
 * Вкладка «Анимации»: клипы, дорожки костей и таймлайн внизу.
 *
 * Модель данных живёт в SkinTool.animation, здесь только интерфейс.
 * Кадр анимации — абсолютное значение (мировая позиция и мировой угол
 * кости), а деформатор принимает пробную позу: поворот как дельту
 * относительно bind-угла, позицию как сдвиг. Конверсия между ними —
 * rotationDelta/rotationAbsolute из core/animation.js, поэтому в полях
 * пользователь всегда видит привычные bind-значения.
 *
 * Всё, что не является частью анимации (выбранный кадр, свёрнутые кости,
 * положение playhead), держим здесь: в экспорт скелета это попадать не должно.
 */
(function (SkinTool) {
  'use strict';

  const $ = (id) => SkinTool.dom.$(id);

  /** Шаг линейки не делаем чаще — иначе подписи сливаются. */
  const MIN_TICK_STEP = 0.05;

  /** Состояние вкладки. */
  const ui = {
    time: 0,
    playing: false,
    lastTickMs: 0,
    loop: true,
    /** Свёрнутые кости. Всё, чего здесь нет, показано раскрытым. */
    collapsed: new Set(),
    trackId: null,
    /**
     * Тип дорожки, который пользователь смотрел последним.
     * Живёт отдельно от trackId: при переходе на другую кость выделенная
     * дорожка меняется, а предпочтение «rotation или position» остаётся.
     */
    preferredKind: 'rotation',
    keyTime: null,
    drag: null
  };

  // ---------- Подключение ----------

  function bind() {
    bindClips();
    bindTracks();
    bindKeyEditor();
    bindPlayback();
    bindTimeline();

    SkinTool.dom.bindDropZone('animDrop', 'animFileInput', readAnimationFile);

    $('btnKeyBone').addEventListener('click', addKeyFromBind);
    $('btnKeyPose').addEventListener('click', addKeyFromTrialPose);

    window.addEventListener('keydown', onKeyDown);

    // Проигрывание живёт в кадровом цикле сцены.
    SkinTool.scene.addFrameListener(onSceneFrame);
  }

  function bindClips() {
    $('animClip').addEventListener('change', (event) => {
      SkinTool.animation.setCurrentClip(event.target.value);
      selectTrack(null);
      setTime(0);
      refresh();
    });

    $('btnAnimNew').addEventListener('click', () => {
      const clip = SkinTool.animation.addClip();

      SkinTool.animation.setCurrentClip(clip.id);
      selectTrack(null);
      setTime(0);
      refresh();
      SkinTool.dom.setStatus(`Создан клип ${clip.id}`);
    });

    $('btnAnimDuplicate').addEventListener('click', duplicateClip);
    $('btnAnimDelete').addEventListener('click', deleteClip);

    $('animClipId').addEventListener('change', (event) => {
      const clip = SkinTool.animation.getCurrentClip();
      const id = event.target.value.trim();

      if (!clip) return;

      clip.id = id || clip.id;
      SkinTool.animation.setCurrentClip(clip.id);
      refresh();
    });

    $('animLength').addEventListener('change', (event) => setLength(event.target.value));
    $('animDockLength').addEventListener('change', (event) => setLength(event.target.value));

    $('animLooped').addEventListener('change', (event) => {
      const clip = SkinTool.animation.getCurrentClip();

      if (clip) clip.looped = event.target.checked;
    });

    $('btnAnimYaml').addEventListener('click', showYaml);
    $('btnAnimYamlAll').addEventListener('click', showAllYaml);
    $('btnAnimCopy').addEventListener('click', copyYaml);
    $('btnAnimDownload').addEventListener('click', downloadYaml);
  }

  function bindTracks() {
    $('animBone').addEventListener('change', (event) => {
      SkinTool.bones.selectBone(Number(event.target.value));
      renderTrackList();
      renderRows();
    });

    $('btnAnimTrackRotation').addEventListener('click', () => addTrack('rotation'));
    $('btnAnimTrackPosition').addEventListener('click', () => addTrack('position'));
  }

  function bindKeyEditor() {
    $('btnAnimKeyUpdate').addEventListener('click', applyKeyEditor);
    $('btnAnimKeyDelete').addEventListener('click', deleteSelectedKey);

    $('animKeyTime').addEventListener('change', applyKeyEditor);
    $('animKeyMode').addEventListener('change', applyKeyEditor);
  }

  function bindPlayback() {
    $('btnAnimPlay').addEventListener('click', play);
    $('btnAnimStop').addEventListener('click', stop);

    $('animTime').addEventListener('change', (event) => {
      setTime(Number(event.target.value) || 0);
    });

    $('animLoopPlayback').addEventListener('change', (event) => {
      ui.loop = event.target.checked;
    });

    $('btnAnimKeyAdd').addEventListener('click', addKeyFromBind);
    $('btnAnimCollapseAll').addEventListener('click', () => setAllCollapsed(true));
    $('btnAnimExpandAll').addEventListener('click', () => setAllCollapsed(false));
  }

  function bindTimeline() {
    const ruler = $('timelineRuler');

    let scrubbing = false;

    // Перемотка по линейке: зажал и тащишь.
    ruler.addEventListener('pointerdown', (event) => {
      scrubbing = true;
      scrubTo(event);
      capturePointer(ruler, event);
    });

    ruler.addEventListener('pointermove', (event) => {
      if (scrubbing) scrubTo(event);
    });

    ruler.addEventListener('pointerup', (event) => {
      scrubbing = false;
      releasePointer(ruler, event);
    });

    bindLaneHeight();

    const rows = $('timelineRows');

    rows.addEventListener('pointermove', onKeyDragMove);
    rows.addEventListener('pointerup', endKeyDrag);
    rows.addEventListener('pointercancel', endKeyDrag);

    // Указатель за пределами окна: Firefox присылает координаты 0,0, и кадр
    // без спроса прыгал на t=0. Пока курсор не в окне, движение игнорируем.
    // Кнопку там отпустить нельзя — событие не дойдёт, поэтому по возврату
    // в окно смотрим на event.buttons: если кнопка уже отжата, перенос закончен.
    window.addEventListener('mouseout', (event) => {
      if (ui.drag && !event.relatedTarget) ui.drag.outside = true;
    });
    window.addEventListener('mouseover', (event) => {
      if (ui.drag && event.relatedTarget) ui.drag.outside = false;
    });
    window.addEventListener('pointermove', (event) => {
      if (ui.drag && !(event.buttons & 1)) endKeyDrag();
    });
    window.addEventListener('blur', () => {
      if (ui.drag) endKeyDrag();
    });
  }

  // ---------- Высота дорожек ----------

  const LANE_HEIGHT_MIN = 12;
  const LANE_HEIGHT_MAX = 64;
  const LANE_HEIGHT_DEFAULT = 21;

  /**
   * Высота дорожек timeline. Живёт в CSS-переменной --tl-lane-height,
   * которую читает .tl-row, поэтому точки и линейка остаются на одной сетке.
   * @returns {number}
   */
  function laneHeight() {
    const inner = $('timelineInner');
    const value = parseFloat(inner.style.getPropertyValue('--tl-lane-height'));

    return Number.isFinite(value) && value > 0 ? value : LANE_HEIGHT_DEFAULT;
  }

  const END_PAD_DEFAULT = 20;

  /**
   * Отступ справа от конца клипа, в пикселях. Живёт в CSS-переменной
   * --tl-end-pad, потому что сетку дорожек рисует CSS, а время по курсору
   * считает JS — оба берут значение оттуда, иначе разъедутся.
   *
   * @returns {number}
   */
  function endPad() {
    const inner = $('timelineInner');
    const value = parseFloat(getComputedStyle(inner).getPropertyValue('--tl-end-pad'));

    return Number.isFinite(value) && value > 0 ? value : END_PAD_DEFAULT;
  }

  /**
   * Позиция во времени вдоль дорожки или линейки.
   *
   * Считаем от «полезной» ширины: всё минус отступ справа. Доля времени
   * уже не до 100% всей дорожки, поэтому последний кадр (t = length)
   * встаёт на её край и остаётся целиком видимым и кликабельным.
   *
   * @param {number} length — длина клипа
   * @param {number} time
   * @returns {string} значение для CSS left
   */
  function timeToLeft(length, time) {
    if (!(length > 0)) return '0px';

    const ratio = Math.min(Math.max(time / length, 0), 1);

    return `calc(${ratio} * (100% - ${endPad()}px))`;
  }

  /** @param {number|null} value */
  function setLaneHeight(value) {
    const height = value === null
      ? LANE_HEIGHT_DEFAULT
      : Math.min(LANE_HEIGHT_MAX, Math.max(LANE_HEIGHT_MIN, Math.round(value)));
    const inner = $('timelineInner');
    const grip = $('timelineHeightGrip');

    inner.style.setProperty('--tl-lane-height', `${height}px`);

    if (grip) {
      grip.title = `Высота дорожек: ${height}px. Потяните за край линейки`;
    }

    return height;
  }

  /**
   * Ползунок высоты: тянем за нижний край линейки. Работает по всей ширине
   * строки, потому что полоса в 7px неудобна для точного попадания.
   */
  function bindLaneHeight() {
    const inner = $('timelineInner');
    const row = $('timelineRulerRow');
    const grip = $('timelineHeightGrip');
    const reset = $('btnTimelineHeightReset');

    if (!row || !grip) return;

    setLaneHeight(null);

    let dragging = false;
    let startY = 0;
    let startHeight = 0;

    grip.addEventListener('pointerdown', (event) => {
      dragging = true;
      startY = event.clientY;
      startHeight = laneHeight();
      grip.classList.add('dragging');
      capturePointer(grip, event);
      event.preventDefault();
    });

    grip.addEventListener('pointermove', (event) => {
      if (!dragging) return;
      setLaneHeight(startHeight + (event.clientY - startY));
    });

    const stop = (event) => {
      if (!dragging) return;
      dragging = false;
      grip.classList.remove('dragging');
      releasePointer(grip, event);
    };

    grip.addEventListener('pointerup', stop);
    grip.addEventListener('pointercancel', stop);

    if (reset) {
      reset.addEventListener('click', () => setLaneHeight(null));
    }
  }

  /**
   * Захват указателя нужен, чтобы кадр не «убегал» из-под курсора на
   * границах дорожки. Синтетические события (и некоторые браузеры) его
   * не поддерживают — поэтому тихо продолжаем без захвата.
   * @param {HTMLElement} element
   * @param {PointerEvent} event
   */
  function capturePointer(element, event) {
    try {
      element.setPointerCapture(event.pointerId);
    } catch (error) {
      // Захват недоступен — перетаскивание всё равно работает по событиям.
    }
  }

  /** @param {HTMLElement} element @param {PointerEvent} event */
  function releasePointer(element, event) {
    try {
      element.releasePointerCapture(event.pointerId);
    } catch (error) {
      // Захвата не было — освобождать нечего.
    }
  }

  /** @param {KeyboardEvent} event */
  function onKeyDown(event) {
    if (SkinTool.model.state.currentTab !== 'animation') return;
    if (isTypingTarget(event.target)) return;

    if (event.code === 'Space') {
      event.preventDefault();

      if (ui.playing) stop(); else play();
    } else if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') {
      // Стрелки на таймлайне двигают playhead, а не якорь кости.
      event.preventDefault();
      stop();
      setTime(ui.time + (event.key === 'ArrowLeft' ? -1 : 1) * (event.shiftKey ? 0.25 : 0.01));
    }
  }

  function isTypingTarget(target) {
    const tag = target.tagName;

    return tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT';
  }

  // ---------- Клипы ----------

  /** @param {string|number} value */
  function setLength(value) {
    const clip = SkinTool.animation.getCurrentClip();
    const length = Math.max(0.05, Number(value) || 1);

    if (!clip) return;

    clip.length = length;
    setTime(Math.min(ui.time, length));
    refresh();
  }

  function duplicateClip() {
    const clip = SkinTool.animation.getCurrentClip();
    if (!clip) return;

    const copy = SkinTool.animation.createClip(SkinTool.animation.nextClipId(), {
      length: clip.length,
      looped: clip.looped
    });

    clip.tracks.forEach((track) => {
      const trackCopy = SkinTool.animation.addTrack(
        copy,
        SkinTool.animation.createTrack(track.boneId, track.kind)
      );

      trackCopy.interpolationMode = track.interpolationMode;
      track.keys.forEach((key) => SkinTool.animation.setKey(trackCopy, key.time, key.value));
    });

    SkinTool.animation.getClips().push(copy);
    SkinTool.animation.setCurrentClip(copy.id);
    selectTrack(null);
    refresh();
    SkinTool.dom.setStatus(`Копия клипа: ${copy.id}`);
  }

  function deleteClip() {
    const clip = SkinTool.animation.getCurrentClip();
    if (!clip) return;

    const clips = SkinTool.animation.getClips();

    if (clips.length === 1 && !confirm('Удалить единственный клип?')) return;

    clips.splice(clips.indexOf(clip), 1);

    SkinTool.animation.setCurrentClip(clips[0] ? clips[0].id : null);
    selectTrack(null);
    setTime(0);
    refresh();
    SkinTool.dom.setStatus(`Клип ${clip.id} удалён`);
  }

  // ---------- Дорожки ----------

  /** @param {'rotation'|'position'} kind */
  function addTrack(kind) {
    const clip = SkinTool.animation.getCurrentClip();
    const bone = SkinTool.model.getSelectedBone();

    if (!clip || !bone) {
      SkinTool.dom.setStatus('Сначала выберите клип и кость');
      return;
    }

    const existing = SkinTool.animation.findTrack(clip, bone.id, kind);

    if (existing) {
      selectTrack(existing);
      refresh();
      SkinTool.dom.setStatus(`Дорожка «${bone.name}» уже есть`);
      return;
    }

    const track = SkinTool.animation.addTrack(clip, SkinTool.animation.createTrack(bone.id, kind));

    // Дорожка без кадров ничего не значит — сразу ставим кадр в текущую позу,
    // иначе добавление дорожки дёрнуло бы кость обратно в bind.
    SkinTool.animation.setKey(track, ui.time, currentValue(bone, kind));

    selectTrack(track);
    expandTo(bone.id);
    refresh();
    SkinTool.dom.setStatus(`Дорожка «${bone.name}»: ${SkinTool.animation.PROPERTIES[kind].label}`);
  }

  /** @returns {object|null} выбранная дорожка */
  function getSelectedTrack() {
    const clip = SkinTool.animation.getCurrentClip();

    if (!clip) return null;

    return clip.tracks.find((track) => track.id === ui.trackId) || null;
  }

  /** @param {object|null} track */
  function selectTrack(track) {
    ui.trackId = track ? track.id : null;
    ui.keyTime = null;

    if (track) ui.preferredKind = track.kind;

    const bone = track ? SkinTool.model.getBone(track.boneId) : null;

    if (bone && SkinTool.model.state.selectedBoneId !== bone.id) {
      SkinTool.bones.selectBone(bone.id);
    }
  }

  // ---------- Ключевые кадры ----------

  /**
   * Значение кости в bind-посе — кадр «из кости».
   * @param {object} bone
   * @param {'rotation'|'position'} kind
   * @returns {number[]}
   */
  /**
   * Значение дорожки для кадра в текущей позе.
   *
   * Берём ровно то, что сейчас на экране: bind-поворот с наложенной
   * дельтой и bind-позиция с наложенным сдвигом. Иначе создание кадра
   * в середине анимации возвращало бы кость в bind-посу — кадр в момент
   * 1 с обнулял бы позу, которая на экране была.
   *
   * @param {object} bone
   * @param {'rotation'|'position'} kind
   * @returns {number[]} градусы или мировая позиция
   */
  function currentValue(bone, kind) {
    const { state } = SkinTool.model;

    if (kind === 'position') {
      const offset = state.poseOffset[bone.id] || [0, 0, 0];

      return [
        bone.position[0] + offset[0],
        bone.position[1] + offset[1],
        bone.position[2] + offset[2]
      ];
    }

    const delta = state.pose[bone.id];

    return delta
      ? SkinTool.animation.rotationAbsolute(bone.rotation, delta)
      : bone.rotation.slice();
  }

  /**
   * Кадр из текущей позы выбранной кости, для всех её дорожек.
   *
   * Значения берутся из того, что сейчас на экране (см. currentValue),
   * поэтому на таймлайне кнопка фиксирует позу в текущей точке времени,
   * а не сбрасывает кость в bind.
   */
  function addKeyFromBind() {
    const clip = SkinTool.animation.getCurrentClip();
    const bone = SkinTool.model.getSelectedBone();

    if (!clip || !bone) {
      SkinTool.dom.setStatus('Сначала выберите клип и кость');
      return;
    }

    let tracks = SkinTool.animation.getBoneTracks(clip, bone.id);

    if (tracks.length === 0) {
      // У кости нет дорожек — создаём обе: bind-поза задаёт и поворот,
      // и позицию, молча создавать только одну значило бы потерять вторую.
      ['rotation', 'position'].forEach((kind) => {
        SkinTool.animation.addTrack(clip, SkinTool.animation.createTrack(bone.id, kind));
      });

      tracks = SkinTool.animation.getBoneTracks(clip, bone.id);
    }

    tracks.forEach((track) => {
      SkinTool.animation.setKey(track, ui.time, currentValue(bone, track.kind));
    });

    expandTo(bone.id);
    refresh();
    SkinTool.dom.setStatus(`Кадр «${bone.name}» на ${ui.time.toFixed(2)} с`);
  }

  /**
   * Кадр из пробной позы вкладки «Позинг». Позинг хранит дельту,
   * а кадр должен быть абсолютным — переводим через rotationAbsolute.
   */
  function addKeyFromTrialPose() {
    const clip = SkinTool.animation.getCurrentClip();
    const bone = SkinTool.model.getSelectedBone();
    const { state } = SkinTool.model;

    if (!clip || !bone) {
      SkinTool.dom.setStatus('Сначала выберите клип и кость');
      return;
    }

    const trialRotation = state.pose[bone.id];
    const trialOffset = state.poseOffset[bone.id];

    if (!trialRotation && !trialOffset) {
      SkinTool.dom.setStatus('Нет пробной позы — настройте её на вкладке «Позинг»');
      return;
    }

    let tracks = SkinTool.animation.getBoneTracks(clip, bone.id);

    if (tracks.length === 0) {
      // Дорожку создаём под то, что действительно настроено: пробное
      // смещение без дорожки позиции иначе потерялось бы молча.
      if (trialRotation) {
        SkinTool.animation.addTrack(clip, SkinTool.animation.createTrack(bone.id, 'rotation'));
      }

      if (trialOffset) {
        SkinTool.animation.addTrack(clip, SkinTool.animation.createTrack(bone.id, 'position'));
      }

      tracks = SkinTool.animation.getBoneTracks(clip, bone.id);
    }

    tracks.forEach((track) => {
      if (track.kind === 'rotation' && trialRotation) {
        SkinTool.animation.setKey(
          track,
          ui.time,
          SkinTool.animation.rotationAbsolute(bone.rotation, trialRotation)
        );
      } else if (track.kind === 'position' && trialOffset) {
        SkinTool.animation.setKey(track, ui.time, [
          bone.position[0] + trialOffset[0],
          bone.position[1] + trialOffset[1],
          bone.position[2] + trialOffset[2]
        ]);
      }
    });

    expandTo(bone.id);
    refresh();
    SkinTool.dom.setStatus(`Кадр из позы «${bone.name}» на ${ui.time.toFixed(2)} с`);
  }

  // Кадр, поставленный из вьюпорта, не требует полной перерисовки.
  let autoKeyRaf = 0;

  /**
   * Синхронизация вьюпорта с кадром: поворот кости кольцом или ползунком
   * сразу записывается в кадр на текущем времени.
   *
   * Пробная поза — дельта к bind, а кадр хранит мировой угол, поэтому
   * значение берём через currentValue. Дорожку создаём на лету: иначе
   * поворот во вьюпорте уходил бы в никуда и кнопка «+ Кадр» потом
   * затирала бы его bind-значением.
   *
   * Перемотка и проигрывание сюда не попадают — они идут через setPose,
   * поэтому чужие кадры при движении времени не переписываются.
   *
   * @param {number} boneId
   */
  function syncRotationToKey(boneId) {
    if (SkinTool.model.state.currentTab !== 'animation') return;

    const clip = SkinTool.animation.getCurrentClip();
    const bone = SkinTool.model.getBone(boneId);

    if (!clip || !bone) return;

    let track = SkinTool.animation.findTrack(clip, bone.id, 'rotation');
    let created = false;

    if (!track) {
      track = SkinTool.animation.addTrack(clip, SkinTool.animation.createTrack(bone.id, 'rotation'));
      expandTo(bone.id);
      created = true;
    }

    SkinTool.animation.setKey(track, ui.time, currentValue(bone, 'rotation'));

    // Если открыт редактор этой дорожки — показываем в нём записанное.
    if (ui.keyTime === null && ui.trackId === track.id) ui.keyTime = ui.time;

    if (created) {
      renderTrackList();
      SkinTool.dom.setStatus(`Дорожка поворота «${bone.name}»: кадр на ${ui.time.toFixed(2)} с`);
    }

    // refresh() здесь не годится: он пересчитывает кадр через setTime, и
    // пробная поза, которую пользователь держит мышью, затёрлась бы. Поэтому
    // трогаем только таймлайн и редактор кадра, и не чаще раза за кадр.
    if (!autoKeyRaf) {
      autoKeyRaf = requestAnimationFrame(() => {
        autoKeyRaf = 0;
        renderRows();
        renderKeyEditor();
      });
    }
  }

  function deleteSelectedKey() {
    const track = getSelectedTrack();
    const key = track ? SkinTool.animation.findKey(track, ui.keyTime) : null;

    if (!track || !key) return;

    SkinTool.animation.removeKey(track, key);
    ui.keyTime = null;
    refresh();
    SkinTool.dom.setStatus('Кадр удалён');
  }

  /** Значения кадра из полей → в модель. */
  function applyKeyEditor() {
    const track = getSelectedTrack();
    const key = track ? SkinTool.animation.findKey(track, ui.keyTime) : null;

    if (!track || !key) return;

    const clip = SkinTool.animation.getCurrentClip();
    const mode = $('animKeyMode').value;

    if (SkinTool.animation.INTERPOLATION_MODES.indexOf(mode) >= 0) {
      track.interpolationMode = mode;
    }

    const inputs = $('animKeyValues').querySelectorAll('input');
    const value = Array.from(inputs).map((input) => Number(input.value) || 0);

    // Значение пишем до переноса кадра: removeKey отсоединяет объект, и
    // запись в него после переноса ушла бы в никуда.
    if (value.length === 3) key.value = value;

    const time = Number($('animKeyTime').value);

    if (Number.isFinite(time) && Math.abs(time - key.time) > 1e-4) {
      const moved = Math.min(Math.max(0, time), clip.length);

      SkinTool.animation.removeKey(track, key);
      // setKey сортирует кадры сам, если время изменилось.
      SkinTool.animation.setKey(track, moved, key.value);
      ui.keyTime = moved;
    }

    applyFrame();
    refresh();
  }

  // ---------- Проигрывание ----------

  function play() {
    if (!SkinTool.animation.getCurrentClip()) return;

    // С конца клипа стартуем сначала — иначе «Старт» ничего не покажет.
    if (ui.time >= clipLength() - 1e-4) setTime(0);

    ui.playing = true;
    ui.lastTickMs = performance.now();
    updatePlayState();
  }

  function stop() {
    if (!ui.playing) return;

    ui.playing = false;
    updatePlayState();
  }

  /** Кадр сцены: двигаем время и пересчитываем позу. */
  function onSceneFrame() {
    if (!ui.playing || document.hidden) return;

    const clip = SkinTool.animation.getCurrentClip();
    if (!clip) {
      stop();
      return;
    }

    const now = performance.now();
    // Ограничиваем шаг: после сворачивания вкладки иначе анимация
    // перепрыгнула бы через пол-клипа за один кадр.
    const delta = Math.min((now - ui.lastTickMs) / 1000, 0.25);

    ui.lastTickMs = now;

    let time = ui.time + delta;

    if (time >= clip.length) {
      if (ui.loop) {
        time = clip.length > 0 ? time % clip.length : 0;
      } else {
        setTime(clip.length);
        stop();
        return;
      }
    }

    setTime(time);
  }

  /** @param {number} time */
  function setTime(time) {
    const length = clipLength();

    ui.time = length > 0 ? Math.min(Math.max(0, time), length) : 0;

    $('animTime').value = ui.time.toFixed(2);
    $('timelineInner').style.setProperty('--tl-time', ui.time / (length || 1));

    applyFrame();
  }

  /** Переносит текущий кадр клипа в деформатор. */
  function applyFrame() {
    const clip = SkinTool.animation.getCurrentClip();

    if (!clip) {
      showBindPose();
      return;
    }

    const sample = SkinTool.animation.sampleClip(clip, ui.time);

    SkinTool.deformer.setPose(sample.pose, sample.offsets);

    // Точки костей стоят в сцене по матрице позы, поэтому при перемотке
    // и проигрывании их нужно двигать вместе с кадром — иначе меш уезжает,
    // а точки остаются в bind-позиции.
    SkinTool.anchors.sync();
  }

  /** Возвращает модель в bind-позу — когда анимации на экране нет. */
  function showBindPose() {
    SkinTool.deformer.discardPose(null);
    SkinTool.anchors.sync();
  }

  function clipLength() {
    const clip = SkinTool.animation.getCurrentClip();

    return clip ? clip.length : 0;
  }

  function updatePlayState() {
    $('animPlayState').textContent = ui.playing ? 'Играет' : 'Стоп';
    $('animPlayState').classList.toggle('playing', ui.playing);
  }

  // ---------- Таймлайн ----------

  /** Дерево строк: кости по иерархии, под раскрытой костью — её дорожки. */
  function renderRows() {
    const container = $('timelineRows');
    const clip = SkinTool.animation.getCurrentClip();
    const { state, getChildBones } = SkinTool.model;

    container.textContent = '';

    const tracksByBone = new Map();

    if (clip) {
      clip.tracks.forEach((track) => {
        if (!tracksByBone.has(track.boneId)) tracksByBone.set(track.boneId, []);

        tracksByBone.get(track.boneId).push(track);
      });
    }

    const length = (clip && clip.length) || 1;

    function appendBone(bone, depth) {
      const tracks = tracksByBone.get(bone.id) || [];
      const children = getChildBones(bone.id);
      const collapsed = ui.collapsed.has(bone.id);

      container.appendChild(buildBoneRow(bone, depth, {
        hasChildren: children.length > 0 || tracks.length > 0,
        collapsed,
        trackCount: tracks.length,
        selected: state.selectedBoneId === bone.id
      }));

      if (collapsed) return;

      tracks.forEach((track) => container.appendChild(buildTrackRow(track, length)));
      children.forEach((child) => appendBone(child, depth + 1));
    }

    getChildBones(null).forEach((bone) => appendBone(bone, 0));
  }

  /**
   * Строка кости.
   * @returns {HTMLElement}
   */
  function buildBoneRow(bone, depth, options) {
    const row = document.createElement('div');

    row.className = 'tl-row bone' + (options.selected ? ' selected-bone' : '');
    row.style.setProperty('--tl-depth', depth);

    const name = document.createElement('div');

    name.className = 'tl-name';
    name.title = bone.name;

    const caret = document.createElement('button');

    caret.className = 'tl-caret' + (options.hasChildren ? '' : ' leaf');
    caret.textContent = options.collapsed ? '▸' : '▾';
    caret.title = options.hasChildren ? 'Развернуть / свернуть' : '';

    caret.addEventListener('click', (event) => {
      event.stopPropagation();
      toggleCollapsed(bone.id);
    });

    const label = document.createElement('span');

    label.textContent = bone.name;

    name.appendChild(caret);
    name.appendChild(label);

    if (options.trackCount > 0) {
      const badge = document.createElement('span');

      badge.className = 'tl-mode';
      badge.textContent = '· ' + options.trackCount;
      name.appendChild(badge);
    }

    name.addEventListener('click', () => {
      SkinTool.bones.selectBone(bone.id);
      renderRows();
      renderTrackList();
    });

    const lane = document.createElement('div');

    lane.className = 'tl-lane';

    // Двойной клик по пустой дорожке кости — завести дорожку поворота.
    lane.addEventListener('dblclick', () => {
      SkinTool.bones.selectBone(bone.id);
      addTrack('rotation');
    });

    row.appendChild(name);
    row.appendChild(lane);

    return row;
  }

  /**
   * Строка дорожки с её ключевыми кадрами.
   * @returns {HTMLElement}
   */
  function buildTrackRow(track, length) {
    const bone = SkinTool.model.getBone(track.boneId);
    const row = document.createElement('div');

    row.className = 'tl-row track' + (track.id === ui.trackId ? ' selected-bone' : '');
    row.dataset.trackId = track.id;
    row.style.setProperty('--tl-depth', depthOf(track.boneId));

    const name = document.createElement('div');

    name.className = 'tl-name';

    const kind = document.createElement('span');

    kind.className = 'tl-kind ' + track.kind;
    kind.textContent = track.kind === 'position' ? 'П' : 'В';
    kind.title = SkinTool.animation.PROPERTIES[track.kind].label;

    const mode = document.createElement('span');

    mode.className = 'tl-mode';
    mode.textContent = track.interpolationMode;
    mode.title = 'InterpolationMode';

    name.appendChild(kind);
    name.appendChild(document.createTextNode(bone ? bone.name : '?'));
    name.appendChild(mode);

    name.addEventListener('click', () => {
      selectTrack(track);
      renderRows();
      renderKeyEditor();
    });

    const lane = document.createElement('div');

    lane.className = 'tl-lane';

    // Двойной клик по дорожке — поставить кадр с текущей позой на это время.
    lane.addEventListener('dblclick', (event) => {
      if (event.target.classList.contains('tl-key')) return;

      const time = timeFromPointer(event, lane, length);

      // Кладём то, что сейчас на экране: иначе двойной клик в середине
      // клипа вбивал бы bind-значение и прыгал позой в этой точке.
      SkinTool.animation.setKey(track, time, currentValue(bone, track.kind));
      selectTrack(track);
      ui.keyTime = time;
      applyFrame();
      refresh();
    });

    track.keys.forEach((key) => lane.appendChild(buildKey(track, key, length)));

    row.appendChild(name);
    row.appendChild(lane);

    return row;
  }

  /**
   * Ромбик ключевого кадра: тянуть можно за сам ромб, время пересчитывается
   * из положения курсора относительно дорожки.
   * @returns {HTMLElement}
   */
  function buildKey(track, key, length) {
    const element = document.createElement('button');

    element.className = 'tl-key ' + track.kind + (isSelectedKey(track, key) ? ' selected' : '');
    element.style.left = timeToLeft(length, key.time);
    element.title = `${key.time.toFixed(2)} с · ${key.value.map((n) => n.toFixed(1)).join(', ')}`;
    element.dataset.trackId = track.id;
    element.dataset.keyTime = String(key.time);

    // Выбираем кадр на pointerdown, а не на click: между ними endKeyDrag
    // перестраивает дерево, и клик уже не дойдёт до элемента.
    element.addEventListener('pointerdown', (event) => {
      event.stopPropagation();

      selectKey(track, key);

      const lane = element.parentElement;

      ui.drag = {
        element,
        track,
        key,
        lane,
        length,
        outside: false,
        // Захват не даёт кадру прыгнуть к курсору в момент нажатия.
        grabOffset: key.time - timeFromPointer(event, lane, length)
      };

      element.classList.add('dragging');
      capturePointer(element, event);
    });

    // Клавиатурная активация кнопки: Enter вызывает click без pointerdown.
    element.addEventListener('click', (event) => {
      event.stopPropagation();

      selectKey(track, key);
    });

    element.addEventListener('contextmenu', (event) => {
      event.preventDefault();
      event.stopPropagation();

      SkinTool.animation.removeKey(track, key);
      ui.keyTime = null;
      refresh();
      SkinTool.dom.setStatus('Кадр удалён');
    });

    return element;
  }

  /** Выбор кадра: подсветка и редактор. Дерево не перестраиваем — это
   * ломало бы перетаскивание, ради которого сюда и пришли указателем. */
  function selectKey(track, key) {
    ui.trackId = track.id;
    ui.keyTime = key.time;

    document.querySelectorAll('#timelineRows .tl-key').forEach((element) => {
      const sameTrack = element.dataset.trackId === track.id;

      element.classList.toggle('selected',
        sameTrack && Math.abs(parseFloat(element.dataset.keyTime) - key.time) < 1e-4);
    });

    document.querySelectorAll('#timelineRows .tl-row.track').forEach((row) => {
      row.classList.toggle('selected-bone', row.dataset.trackId === track.id);
    });

    renderKeyEditor();
  }

  /** @param {object} track @param {object} key */
  function isSelectedKey(track, key) {
    return track.id === ui.trackId && ui.keyTime !== null && Math.abs(key.time - ui.keyTime) < 1e-4;
  }

  /** @param {PointerEvent} event */
  function onKeyDragMove(event) {
    if (!ui.drag || ui.drag.outside) return;

    const { lane, length, grabOffset } = ui.drag;
    const time = Math.min(
      Math.max(0, timeFromPointer(event, lane, length) + grabOffset),
      length
    );

    ui.drag.key.time = time;
    ui.drag.track.keys.sort((a, b) => a.time - b.time);
    ui.drag.element.style.left = timeToLeft(length, time);
    ui.drag.element.title = time.toFixed(2) + ' с';

    ui.keyTime = time;
    setTime(time);
  }

  function endKeyDrag() {
    if (!ui.drag) return;

    ui.drag.element.classList.remove('dragging');
    ui.drag = null;

    renderRows();
    renderKeyEditor();
  }

  /**
   * Время по положению курсора над дорожкой.
   *
   * Обратная операция к timeToLeft: полезная ширина — всё минус отступ
   * справа, поэтому курсор в отступе даёт время больше длины клипа и мы
   * прижимаем его к концу.
   *
   * @param {PointerEvent|MouseEvent} event
   * @param {HTMLElement} lane
   * @param {number} length
   * @returns {number}
   */
  function timeFromPointer(event, lane, length) {
    const rect = lane.getBoundingClientRect();
    const usable = rect.width - endPad();

    if (usable <= 0) return 0;

    const ratio = (event.clientX - rect.left) / usable;

    return Math.min(Math.max(0, ratio), 1) * length;
  }

  /** @param {PointerEvent} event */
  function scrubTo(event) {
    stop();
    setTime(timeFromPointer(event, $('timelineRuler'), clipLength()));
  }

  /** Линейка времени: шаг делений выбираем «красивым». */
  function renderRuler() {
    const ruler = $('timelineRuler');
    const clip = SkinTool.animation.getCurrentClip();

    ruler.textContent = '';

    if (!clip || clip.length <= 0) return;

    const step = tickStep(clip.length);
    const count = Math.floor(clip.length / step + 1e-6);

    for (let i = 0; i <= count; i++) {
      const time = i * step;
      const tick = document.createElement('div');
      const isWhole = Math.abs(time - Math.round(time)) < 1e-6;

      tick.className = 'tl-tick' + (isWhole ? ' major' : '');
      tick.style.left = timeToLeft(clip.length, time);

      const label = document.createElement('span');

      label.className = 'tl-tick-label';
      label.textContent = isWhole ? String(time) : String(Number(time.toFixed(2)));
      tick.appendChild(label);

      ruler.appendChild(tick);
    }
  }

  /**
   * Шаг делений линейки: из «красивых» значений, но не чаще MIN_TICK_STEP.
   * @param {number} length
   * @returns {number}
   */
  function tickStep(length) {
    const candidates = [0.05, 0.1, 0.2, 0.25, 0.5, 1, 2, 5, 10, 15, 30, 60];
    const target = length / 12;
    const step = candidates.find((value) => value >= target) || candidates[candidates.length - 1];

    return Math.max(step, MIN_TICK_STEP);
  }

  // ---------- Панель ----------

  function renderClipList() {
    const select = $('animClip');
    const clips = SkinTool.animation.getClips();
    const current = SkinTool.animation.getCurrentClip();

    select.textContent = '';

    if (clips.length === 0) {
      const option = document.createElement('option');

      option.textContent = '— клипов нет —';
      select.appendChild(option);
      select.disabled = true;
      return;
    }

    select.disabled = false;

    clips.forEach((clip) => {
      const option = document.createElement('option');

      option.value = clip.id;
      option.textContent = `${clip.id} · ${clip.length.toFixed(2)} с · ${clip.tracks.length} дор.`;
      option.selected = Boolean(current) && clip.id === current.id;
      select.appendChild(option);
    });
  }

  function renderClipFields() {
    const clip = SkinTool.animation.getCurrentClip();

    $('animClipId').value = clip ? clip.id : '';
    $('animLength').value = clip ? clip.length : '';
    $('animDockLength').value = clip ? clip.length : '';
    $('animLooped').checked = clip ? clip.looped : true;
    $('animLooped').disabled = !clip;
    $('btnAnimDelete').disabled = !clip;
  }

  function renderBoneList() {
    const select = $('animBone');
    const { state } = SkinTool.model;

    select.textContent = '';

    if (state.bones.length === 0) {
      const option = document.createElement('option');

      option.textContent = '— костей нет —';
      select.appendChild(option);
      select.disabled = true;
      return;
    }

    select.disabled = false;

    // Порядок как в скелете — с отступом по глубине, а не сортировкой.
    state.bones.forEach((bone) => {
      const option = document.createElement('option');

      option.value = bone.id;
      option.textContent = '  '.repeat(depthOf(bone.id)) + bone.name;
      option.selected = state.selectedBoneId === bone.id;
      select.appendChild(option);
    });
  }

  /**
   * Глубина кости в дереве — для отступа в списке выбора и в строках.
   * @param {number} boneId
   * @returns {number}
   */
  function depthOf(boneId) {
    let depth = 0;
    let bone = SkinTool.model.getBone(boneId);

    while (bone && bone.parentId !== null && depth < 32) {
      bone = SkinTool.model.getBone(bone.parentId);
      depth++;
    }

    return depth;
  }

  function renderTrackList() {
    const list = $('animTrackList');
    const clip = SkinTool.animation.getCurrentClip();
    const bone = SkinTool.model.getSelectedBone();

    list.textContent = '';

    if (!clip || !bone) return;

    const tracks = SkinTool.animation.getBoneTracks(clip, bone.id);

    if (tracks.length === 0) {
      const hint = document.createElement('div');

      hint.className = 'hint';
      hint.textContent = 'У кости нет дорожек — добавьте поворот или позицию.';
      list.appendChild(hint);
      return;
    }

    tracks.forEach((track) => {
      const item = document.createElement('div');

      item.className = 'anim-track-item';

      const title = document.createElement('span');

      title.className = 'anim-track-name';
      title.textContent = `${SkinTool.animation.PROPERTIES[track.kind].label} · ${track.keys.length} кадр.`;

      const modeButton = document.createElement('button');

      modeButton.textContent = track.interpolationMode;
      modeButton.title = 'Сменить InterpolationMode';
      modeButton.addEventListener('click', () => cycleMode(track));

      const remove = document.createElement('button');

      remove.textContent = '🗑';
      remove.title = 'Удалить дорожку';
      remove.addEventListener('click', () => {
        clip.tracks.splice(clip.tracks.indexOf(track), 1);

        if (ui.trackId === track.id) selectTrack(null);

        refresh();
      });

      item.appendChild(title);
      item.appendChild(modeButton);
      item.appendChild(remove);
      list.appendChild(item);
    });
  }

  /** @param {object} track */
  function cycleMode(track) {
    const modes = SkinTool.animation.INTERPOLATION_MODES;
    const index = modes.indexOf(track.interpolationMode);

    track.interpolationMode = modes[(index + 1) % modes.length];

    refresh();
    SkinTool.dom.setStatus(`InterpolationMode: ${track.interpolationMode}`);
  }

  function renderKeyEditor() {
    const track = getSelectedTrack();
    const key = track ? SkinTool.animation.findKey(track, ui.keyTime) : null;

    $('animKeyEmpty').classList.toggle('hidden', Boolean(key));
    $('animKeyEditor').classList.toggle('hidden', !key);

    if (!track || !key) return;

    const bone = SkinTool.model.getBone(track.boneId);

    $('animKeyTime').value = key.time.toFixed(2);
    fillModeSelect(track.interpolationMode);
    renderModeHint(track.interpolationMode);
    renderKeyValues(track, bone, key);
  }

  /** @param {string} mode */
  function fillModeSelect(mode) {
    const select = $('animKeyMode');

    if (select.options.length === 0) {
      SkinTool.animation.INTERPOLATION_MODES.forEach((value) => {
        const option = document.createElement('option');

        option.value = value;
        option.textContent = value;
        select.appendChild(option);
      });
    }

    select.value = mode;
  }

  /** @param {string} mode */
  function renderModeHint(mode) {
    const hints = {
      Linear: 'Ровная линия между соседними кадрами. Это то, что движок берёт по умолчанию.',
      Cubic: 'Плавная кривая Catmull-Rom через все кадры.',
      Nearest: 'Значение ближайшего по времени кадра — ступеньками.',
      Previous: 'Держится значение предыдущего кадра — «залипает».'
    };

    $('animKeyModeHint').textContent = hints[mode] || '';
  }

  /**
   * Поля значения кадра: и для угла, и для позиции это три числа.
   * @param {object} track
   * @param {object|null} bone
   * @param {object} key
   */
  function renderKeyValues(track, bone, key) {
    const container = $('animKeyValues');

    container.textContent = '';

    ['X', 'Y', 'Z'].forEach((axis, index) => {
      const row = document.createElement('div');

      row.className = 'anim-value-row';

      const label = document.createElement('label');

      label.textContent = axis;

      const input = document.createElement('input');

      input.type = 'number';
      input.step = track.kind === 'rotation' ? '1' : '0.01';
      input.value = Number(key.value[index].toFixed(4));

      row.appendChild(label);
      row.appendChild(input);
      container.appendChild(row);
    });

    const hint = document.createElement('div');

    hint.className = 'hint';

    if (!bone) {
      hint.textContent = 'Кость удалена — дорожка будет удалена при следующем обновлении.';
    } else if (track.kind === 'rotation') {
      hint.textContent = `Абсолютный угол, градусы. Bind-угол кости: `
        + bone.rotation.map((value) => value.toFixed(0)).join(', ') + '°';
    } else {
      hint.textContent = `Позиция в мире, единицы модели. Bind: `
        + bone.position.map((value) => value.toFixed(2)).join(', ');
    }

    container.appendChild(hint);
  }

  // ---------- Раскрытие дерева ----------

  /** @param {number} boneId */
  function toggleCollapsed(boneId) {
    if (ui.collapsed.has(boneId)) {
      ui.collapsed.delete(boneId);
    } else {
      ui.collapsed.add(boneId);
    }

    renderRows();
  }

  /** @param {boolean} collapsed */
  function setAllCollapsed(collapsed) {
    const { state } = SkinTool.model;

    ui.collapsed = collapsed
      ? new Set(state.bones.map((bone) => bone.id))
      : new Set();

    renderRows();
  }

  /** Открывает путь от корня до кости — иначе её дорожки не видно. */
  function expandTo(boneId) {
    let bone = SkinTool.model.getBone(boneId);

    while (bone && bone.parentId !== null) {
      ui.collapsed.delete(bone.parentId);
      bone = SkinTool.model.getBone(bone.parentId);
    }
  }

  // ---------- Импорт и экспорт ----------

  function showYaml() {
    const clip = SkinTool.animation.getCurrentClip();

    $('animYamlOutput').textContent = clip ? SkinTool.animation.toYaml(clip) : '';
  }

  function showAllYaml() {
    $('animYamlOutput').textContent = SkinTool.animation.toYamlAll();
  }

  function currentYaml() {
    const shown = $('animYamlOutput').textContent.trim();

    if (shown) return shown;

    const clip = SkinTool.animation.getCurrentClip();

    return clip ? SkinTool.animation.toYaml(clip) : '';
  }

  function copyYaml() {
    const text = currentYaml();

    if (!text) {
      SkinTool.dom.setStatus('Нечего копировать');
      return;
    }

    // На file:// буфер обмена недоступен — молчаливый фейл хуже подсказки.
    if (!navigator.clipboard) {
      SkinTool.dom.setStatus('Буфер обмена недоступен — используйте «Скачать»');
      return;
    }

    navigator.clipboard.writeText(text)
      .then(() => SkinTool.dom.setStatus('YAML скопирован'))
      .catch(() => SkinTool.dom.setStatus('Не удалось скопировать — используйте «Скачать»'));
  }

  function downloadYaml() {
    const clip = SkinTool.animation.getCurrentClip();

    if (!clip) return;

    const name = SkinTool.animationYaml.buildFileName(clip);
    const blob = new Blob([SkinTool.animation.toYaml(clip)], { type: 'text/yaml' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');

    link.href = url;
    link.download = name;
    link.click();
    URL.revokeObjectURL(url);

    SkinTool.dom.setStatus(`Скачан ${name}`);
  }

  /** @param {File} file */
  function readAnimationFile(file) {
    const reader = new FileReader();

    reader.onload = (event) => {
      try {
        const result = SkinTool.animationYaml.importYAML(event.target.result);

        selectTrack(null);
        setTime(0);
        refresh();

        $('animImportLog').textContent =
          `Из ${file.name}: добавлено ${result.added}, заменено ${result.replaced}`
          + (result.skipped.length > 0 ? `, пропущено строк: ${result.skipped.length}` : '');

        SkinTool.dom.setStatus(`Импортировано клипов: ${result.added + result.replaced}`);
      } catch (error) {
        $('animImportLog').textContent = 'Ошибка: ' + error.message;
        console.error(error);
      }
    };

    reader.readAsText(file);
  }

  // ---------- Обновление ----------

  /** Полная перерисовка вкладки. */
  function refresh() {
    renderClipList();
    renderClipFields();
    renderBoneList();
    renderTrackList();
    renderRuler();
    renderRows();
    renderKeyEditor();
    updatePlayState();

    // YAML блок показывает снимок на момент нажатия — при смене клипа он
    // устаревает, поэтому чистим.
    $('animYamlOutput').textContent = '';

    setTime(Math.min(ui.time, clipLength()));
  }

  /**
   * Подбирает дорожку кости для панели ключей и выделяет её.
   *
   * Вызывается, когда кость выбрали во вьюпорте или в списке: панель
   * должна показать ключи именно этой кости, иначе выбор в 3D и в
   * timeline расходится. Если у кости в текущем клипе нет дорожек —
   * выделение снимается.
   *
   * При двух дорожках кости держим тип последней выбранной (rotation или
   * position), чтобы переключение «поворот ↔ позиция» не сбрасывалось
   * при каждом клике по кости; при отсутствии такого типа берём rotation.
   *
   * @param {number} boneId
   */
  function selectTrackForBone(boneId) {
    const clip = SkinTool.animation.getCurrentClip();

    if (!clip) {
      if (ui.trackId !== null) selectTrack(null);
      renderRows();
      renderKeyEditor();
      return;
    }

    const tracks = clip.tracks.filter((track) => track.boneId === boneId);

    if (tracks.length === 0) {
      if (ui.trackId !== null) selectTrack(null);
      renderRows();
      renderKeyEditor();
      return;
    }

    const kind = ui.preferredKind;
    const track = tracks.find((item) => item.kind === kind)
      || tracks.find((item) => item.kind === 'rotation')
      || tracks[0];

    if (!track || track.id === ui.trackId) return;

    selectTrack(track);

    // Подбор дорожки не должен менять предпочтение типа: у кости может не
    // оказаться rotation, и тогда мы подставили position, но пользователь
    // по-прежнему смотрит повороты и хочет их на следующей кости.
    ui.preferredKind = kind;

    renderRows();
    renderKeyEditor();
  }

  SkinTool.animationUI = {
    bind,
    refresh,
    play,
    stop,
    showBindPose,
    syncRotationToKey,
    selectTrackForBone,
    getLaneHeight: laneHeight,
    setLaneHeight,
    getTime: () => ui.time
  };
})(window.SkinTool);
### Локализация для консольных команд движка

cmd-hint-float = [дробное число]

## Общие ошибки команд

cmd-invalid-arg-number-error = Неверное количество аргументов.

cmd-parse-failure-integer = {$arg} не является корректным целым числом.
cmd-parse-failure-float = {$arg} не является корректным дробным числом.
cmd-parse-failure-bool = {$arg} не является корректным логическим значением.
cmd-parse-failure-uid = {$arg} не является корректным UID сущности.
cmd-parse-failure-mapid = {$arg} не является корректным MapId.
cmd-parse-failure-enum = {$arg} не является допустимым значением перечисления {$enum}.
cmd-parse-failure-grid = {$arg} не является корректной сеткой.
cmd-parse-failure-cultureinfo = "{$arg}" не является корректным значением CultureInfo.
cmd-parse-failure-entity-exist = UID {$arg} не соответствует существующей сущности.
cmd-parse-failure-session = Сессия с именем пользователя: {$username} не найдена.

cmd-error-file-not-found = Файл не найден: {$file}.
cmd-error-dir-not-found = Директория не найдена: {$dir}.

cmd-failure-no-attached-entity = К этой оболочке не привязана ни одна сущность.

## Команда 'help'
cmd-help-desc = Отображает общую справку или справку по конкретной команде.
cmd-help-help = Использование: {$command} [имя_команды]
    Если имя команды не указано, отображается общая справочная информация. Если указано имя команды, отображается справка по этой команде.

cmd-help-no-args = Чтобы получить справку по конкретной команде, введите 'help <команда>'. Чтобы вывести список всех доступных команд, введите 'list'. Для поиска команд используйте 'list <фильтр>'.
cmd-help-unknown = Неизвестная команда: { $command }
cmd-help-top = { $command } — { $description }
cmd-help-invalid-args = Неверное количество аргументов.
cmd-help-arg-cmdname = [имя_команды]

## Команда 'cvar'
cmd-cvar-desc = Получает или устанавливает значение CVar.
cmd-cvar-help = Использование: {$command} <имя | ?>
    Если передано значение, оно анализируется и сохраняется как новое значение CVar.
    Если значение не передано, отображается текущее значение CVar.
    Используйте 'cvar ?' для получения списка всех зарегистрированных CVars.

cmd-cvar-invalid-args = Требуется ровно один или два аргумента.
cmd-cvar-not-registered = CVar '{ $cvar }' не зарегистрирован. Используйте 'cvar ?' для получения списка всех зарегистрированных CVars.
cmd-cvar-parse-error = Введённое значение имеет неверный формат для типа { $type }
cmd-cvar-compl-list = Показать список доступных CVars
cmd-cvar-arg-name = <имя | ?>
cmd-cvar-value-hidden = <значение скрыто>

## Команда 'cvar_subs'
cmd-cvar_subs-desc = Выводит список подписок OnValueChanged для CVar.
cmd-cvar_subs-help = Использование: {$command} <имя>

cmd-cvar_subs-invalid-args = Требуется ровно один аргумент.
cmd-cvar_subs-arg-name = <имя>

## Команда 'list'
cmd-list-desc = Выводит список доступных команд с возможностью фильтрации.
cmd-list-help = Использование: {$command} [фильтр]
    Выводит все доступные команды. Если указан аргумент, он используется для фильтрации команд по имени.

cmd-list-heading = ИМЯ КОМАНДЫ            ОПИСАНИЕ{"\u000A"}-------------------------{"\u000A"}

cmd-list-arg-filter = [фильтр]

## Команда '>', также известная как удалённое выполнение (remote exec)
cmd-remoteexec-desc = Выполняет команды на стороне сервера.
cmd-remoteexec-help = Использование: > <команда> [арг] [арг] [арг...]
    Выполняет команду на сервере. Это необходимо, если команда с тем же именем существует на клиенте, так как при обычном вызове будет запущена клиентская команда.

## Команда 'gc'
cmd-gc-desc = Запускает сборщик мусора (Garbage Collector).
cmd-gc-help = Использование: {$command} [поколение]
    Использует GC.Collect() для выполнения сборки мусора.
    Если указан аргумент, он интерпретируется как номер поколения GC, и вызывается GC.Collect(int).
    Используйте команду 'gfc', чтобы выполнить полную сборку мусора с компактификацией LOH.

cmd-gc-failed-parse = Не удалось разобрать аргумент.
cmd-gc-arg-generation = [поколение]

## Команда 'gcf'
cmd-gcf-desc = Запускает полную сборку мусора с компактификацией LOH и всего остального.
cmd-gcf-help = Использование: {$command}
    Выполняет полную сборку GC.Collect(2, GCCollectionMode.Forced, true, true) с компактификацией LOH.
    Это может привести к зависанию на сотни миллисекунд — будьте осторожны.

## Команда 'gc_mode'
cmd-gc_mode-desc = Изменяет или отображает режим задержки сборщика мусора (GC Latency mode).
cmd-gc_mode-help = Использование: {$command} [тип]
    Если аргумент не указан, возвращается текущий режим задержки GC.
    Если аргумент указан, он интерпретируется как GCLatencyMode и устанавливается в качестве нового режима задержки GC.

cmd-gc_mode-current = текущий режим задержки GC: { $prevMode }
cmd-gc_mode-possible = возможные режимы:
cmd-gc_mode-option = - { $mode }
cmd-gc_mode-unknown = неизвестный режим задержки GC: { $arg }
cmd-gc_mode-attempt = попытка изменения режима задержки GC: { $prevMode } -> { $mode }
cmd-gc_mode-result = итоговый режим задержки GC: { $mode }
cmd-gc_mode-arg-type = [тип]

## Команда 'mem'
cmd-mem-desc = Выводит информацию об управляемой памяти.
cmd-mem-help = Использование: {$command}

cmd-mem-report = Размер кучи: { TOSTRING($heapSize, "N0") }
    Всего выделено: { TOSTRING($totalAllocated, "N0") }

## Команда 'physics'
cmd-physics-overlay = {$overlay} не является распознанным оверлеем

## Команда 'lsasm'
cmd-lsasm-desc = Выводит список загруженных сборок по контекстам загрузки.
cmd-lsasm-help = Использование: lsasm

## Команда 'exec'
cmd-exec-desc = Выполняет файл сценария из записываемых пользовательских данных игры.
cmd-exec-help = Использование: {$command} <имяФайла>
    Каждая строка в файле выполняется как отдельная команда, за исключением строк, начинающихся с символа #.

cmd-exec-arg-filename = <имяФайла>

## Команда 'dump_net_comps'
cmd-dump_net_comps-desc = Выводит таблицу сетевых компонентов.
cmd-dump_net_comps-help = Использование: {$command}

cmd-dump_net_comps-error-writeable = Регистрация ещё открыта для записи, сетевые ID не были сгенерированы.
cmd-dump_net_comps-header = Регистрации сетевых компонентов:

## Команда 'dump_event_tables'
cmd-dump_event_tables-desc = Выводит таблицы направленных событий для сущности.
cmd-dump_event_tables-help = Использование: {$command} <entityUid>

cmd-dump_event_tables-missing-arg-entity = Отсутствует аргумент сущности
cmd-dump_event_tables-error-entity = Недопустимая сущность
cmd-dump_event_tables-arg-entity = <entityUid>

## Команда 'monitor'
cmd-monitor-desc = Переключает отладочный монитор в меню F3.
cmd-monitor-help = Использование: {$command} <имя>
    Возможные мониторы: { $monitors }
    Также можно использовать специальные значения "-all" и "+all" для скрытия или отображения всех мониторов соответственно.

cmd-monitor-arg-monitor = <монитор>
cmd-monitor-invalid-name = Недопустимое имя монитора
cmd-monitor-arg-count = Отсутствует аргумент монитора
cmd-monitor-minus-all-hint = Скрывает все мониторы
cmd-monitor-plus-all-hint = Отображает все мониторы

## Команда 'setambientlight'
cmd-set-ambient-light-desc = Позволяет установить уровень фонового освещения для указанной карты в цветовом пространстве sRGB.
cmd-set-ambient-light-help = Использование: {$command} [mapid] [r g b a]
cmd-set-ambient-light-parse = Не удалось разобрать аргументы как байтовые значения цвета.

## Команды работы с картами

cmd-savemap-desc = Сериализует карту на диск. Не сохраняет карту после инициализации, если не принудительно указано.
cmd-savemap-help = Использование: {$command} <MapID> <Путь> [force]
cmd-savemap-not-exist = Целевая карта не существует.
cmd-savemap-init-warning = Попытка сохранить карту после инициализации без принудительного сохранения.
cmd-savemap-attempt = Попытка сохранить карту {$mapId} в {$path}.
cmd-savemap-success = Карта успешно сохранена.
cmd-savemap-error = Не удалось сохранить карту! Подробности см. в журнале сервера.
cmd-hint-savemap-id = <MapID>
cmd-hint-savemap-path = <Путь>
cmd-hint-savemap-force = [логическое значение]

cmd-loadmap-desc = Загружает карту с диска в игру.
cmd-loadmap-help = Использование: {$command} <MapID> <Путь> [x] [y] [поворот] [consistentUids]
cmd-loadmap-nullspace = Нельзя загружать в карту 0.
cmd-loadmap-exists = Карта {$mapId} уже существует.
cmd-loadmap-success = Карта {$mapId} загружена из {$path}.
cmd-loadmap-error = Произошла ошибка при загрузке карты из {$path}.
cmd-hint-loadmap-x-position = [координата X]
cmd-hint-loadmap-y-position = [координата Y]
cmd-hint-loadmap-rotation = [поворот]
cmd-hint-loadmap-uids = [дробное число]

cmd-hint-savebp-id = <ID сетки сущности>

## Команда 'flushcookies'
# Примечание: команда flushcookies относится к Robust.Client.WebView и не входит в основной код движка.

cmd-flushcookies-desc = Сохраняет хранилище cookies CEF на диск.
cmd-flushcookies-help = Использование: {$command}
    Это гарантирует корректное сохранение cookies на диск в случае некорректного завершения работы.
    Обратите внимание, что операция выполняется асинхронно.

cmd-ldrsc-desc = Предварительно кэширует ресурс.
cmd-ldrsc-help = Использование: {$command} <путь> <тип>

cmd-rldrsc-desc = Перезагружает ресурс.
cmd-rldrsc-help = Использование: {$command} <путь> <тип>

cmd-gridtc-desc = Получает количество тайлов в сетке.
cmd-gridtc-help = Использование: {$command} <gridId>

# Команды на стороне клиента
cmd-guidump-desc = Сохраняет дерево GUI в файл /guidump.txt в пользовательских данных.
cmd-guidump-help = Использование: {$command}

cmd-uitest-desc = Открывает тестовое окно пользовательского интерфейса.
cmd-uitest-help = Использование: {$command}

## Команда 'uitest2'
cmd-uitest2-desc = Открывает окно ОС для тестирования элементов управления UI.
cmd-uitest2-help = Использование: {$command} <вкладка>
cmd-uitest2-arg-tab = <вкладка>
cmd-uitest2-error-args = Ожидается не более одного аргумента
cmd-uitest2-error-tab = Недопустимая вкладка: '{$value}'
cmd-uitest2-title = UITest2

cmd-setclipboard-desc = Устанавливает содержимое системного буфера обмена.
cmd-setclipboard-help = Использование: {$command} <текст>

cmd-getclipboard-desc = Получает содержимое системного буфера обмена.
cmd-getclipboard-help = Использование: {$command}

cmd-togglelight-desc = Переключает отрисовку освещения.
cmd-togglelight-help = Использование: {$command}

cmd-togglefov-desc = Переключает поле зрения (FOV) на клиенте.
cmd-togglefov-help = Использование: {$command}

cmd-togglehardfov-desc = Переключает жёсткое поле зрения (hard FOV) на клиенте. (для отладки space-station-14#2353)
cmd-togglehardfov-help = Использование: {$command}

cmd-toggleshadows-desc = Переключает отрисовку теней.
cmd-toggleshadows-help = Использование: {$command}

cmd-togglelightbuf-desc = Переключает отрисовку освещения. Включает тени, но не FOV.
cmd-togglelightbuf-help = Использование: {$command}

cmd-chunkinfo-desc = Получает информацию о чанке под курсором мыши.
cmd-chunkinfo-help = Использование: {$command}

cmd-rldshader-desc = Перезагружает все шейдеры.
cmd-rldshader-help = Использование: {$command}

cmd-cldbglyr-desc = Переключает отладочные слои поля зрения и освещения.
cmd-cldbglyr-help= Использование: {$command} <слой>: переключить <слой>
    cldbglyr: отключить все слои

cmd-key-info-desc = Получает информацию о клавише.
cmd-key-info-help = Использование: {$command} <Клавиша>

## Команда 'bind'
cmd-bind-desc = Назначает комбинацию клавиш на команду ввода.
cmd-bind-help = Использование: {$command} { cmd-bind-arg-key } { cmd-bind-arg-mode } { cmd-bind-arg-command }
    Обратите внимание, что это НЕ сохраняет назначения автоматически.
    Используйте команду 'svbind' для сохранения конфигурации назначений.

cmd-bind-arg-key = <ИмяКлавиши>
cmd-bind-arg-mode = <РежимНазначения>
cmd-bind-arg-command = <КомандаВвода>

cmd-net-draw-interp-desc = Переключает отладочную отрисовку сетевой интерполяции.
cmd-net-draw-interp-help = Использование: {$command}

cmd-net-watch-ent-desc = Выводит все сетевые обновления для EntityId в консоль.
cmd-net-watch-ent-help = Использование: {$command} <0|EntityUid>

cmd-net-refresh-desc = Запрашивает полное состояние сервера.
cmd-net-refresh-help = Использование: {$command}

cmd-net-entity-report-desc = Переключает панель отчёта по сетевым сущностям.
cmd-net-entity-report-help = Использование: {$command}

cmd-fill-desc = Заполняет консоль для отладки.
cmd-fill-help = Использование: {$command}
                Заполняет консоль случайным текстом для отладки.

cmd-cls-desc = Очищает консоль.
cmd-cls-help = Использование: {$command}
               Очищает отладочную консоль от всех сообщений.

cmd-sendgarbage-desc = Отправляет мусор на сервер.
cmd-sendgarbage-help = Использование: {$command}
                       Сервер ответит: 'no u'

cmd-loadgrid-desc = Загружает сетку из файла в существующую карту.
cmd-loadgrid-help = Использование: {$command} <MapID> <Путь> [x y] [поворот] [storeUids]

cmd-loc-desc = Выводит абсолютное местоположение сущности игрока в консоль.
cmd-loc-help = Использование: {$command}

cmd-tpgrid-desc = Телепортирует сетку в новое местоположение.
cmd-tpgrid-help = Использование: {$command} <gridId> <X> <Y> [<MapId>]

cmd-rmgrid-desc = Удаляет сетку с карты. Нельзя удалить сетку по умолчанию.
cmd-rmgrid-help = Использование: {$command} <gridId>

cmd-mapinit-desc = Запускает инициализацию карты.
cmd-mapinit-help = Использование: {$command} <mapID>

cmd-lsmap-desc = Выводит список карт.
cmd-lsmap-help = Использование: {$command}

cmd-lsgrid-desc = Выводит список сеток.
cmd-lsgrid-help = Использование: {$command}

cmd-addmap-desc = Добавляет новую пустую карту в раунд. Если mapID уже существует, команда ничего не делает.
cmd-addmap-help = Использование: {$command} <mapID> [pre-init]

cmd-rmmap-desc = Удаляет карту из мира. Нельзя удалить nullspace.
cmd-rmmap-help = Использование: {$command} <mapId>

cmd-savegrid-desc = Сериализует сетку на диск.
cmd-savegrid-help = Использование: {$command} <gridID> <Путь>

cmd-testbed-desc = Загружает тестовую площадку физики на указанную карту.
cmd-testbed-help = Использование: {$command} <mapid> <тест>

## Команда 'addcomp'
cmd-addcomp-desc = Добавляет компонент к сущности.
cmd-addcomp-help = Использование: {$command} <uid> <имяКомпонента>
cmd-addcompc-desc = Добавляет компонент к сущности на клиенте.
cmd-addcompc-help = Использование: {$command} <uid> <имяКомпонента>

## Команда 'rmcomp'
cmd-rmcomp-desc = Удаляет компонент у сущности.
cmd-rmcomp-help = Использование: {$command} <uid> <имяКомпонента>
cmd-rmcompc-desc = Удаляет компонент у сущности на клиенте.
cmd-rmcompc-help = Использование: {$command} <uid> <имяКомпонента>

## Команда 'addview'
cmd-addview-desc = Позволяет подписаться на представление сущности в целях отладки.
cmd-addview-help = Использование: {$command} <entityUid>
cmd-addviewc-desc = Позволяет подписаться на представление сущности в целях отладки.
cmd-addviewc-help = Использование: {$command} <entityUid>

## Команда 'removeview'
cmd-removeview-desc = Позволяет отписаться от представления сущности в целях отладки.
cmd-removeview-help = Использование: {$command} <entityUid>

## Команда 'loglevel'
cmd-loglevel-desc = Изменяет уровень логирования для указанной пилорамы (sawmill).
cmd-loglevel-help = Использование: {$command} <пилорама> <уровень>
      пилорама: метка, предваряющая сообщения журнала. Именно для неё устанавливается уровень.
      уровень: уровень логирования. Должен соответствовать одному из значений перечисления LogLevel.

cmd-testlog-desc = Записывает тестовое сообщение в журнал пилорамы.
cmd-testlog-help = Использование: {$command} <пилорама> <уровень> <сообщение>
    пилорама: метка, предваряющая сообщение в журнале.
    уровень: уровень логирования. Должен соответствовать одному из значений перечисления LogLevel.
    сообщение: записываемое сообщение. Заключите его в двойные кавычки, если хотите использовать пробелы.

## Команда 'vv'
cmd-vv-desc = Открывает просмотр переменных (View Variables).
cmd-vv-help = Использование: {$command} <ID сущности|имя интерфейса IoC|имя интерфейса SIoC>

## Команда 'showvelocities'
cmd-showvelocities-desc = Отображает угловую и линейную скорости.
cmd-showvelocities-help = Использование: {$command}

## Команда 'setinputcontext'
cmd-setinputcontext-desc = Устанавливает активный контекст ввода.
cmd-setinputcontext-help = Использование: {$command} <контекст>

## Команда 'forall'
cmd-forall-desc = Выполняет команду для всех сущностей, имеющих указанный компонент.
cmd-forall-help = Использование: {$command} <запрос BQL> do <команда...>

## Команда 'delete'
cmd-delete-desc = Удаляет сущность с указанным ID.
cmd-delete-help = Использование: {$command} <UID сущности>

# Системные команды
cmd-showtime-desc = Показывает серверное время.
cmd-showtime-help = Использование: {$command}

cmd-restart-desc = Корректно перезапускает сервер (не только раунд).
cmd-restart-help = Использование: {$command}

cmd-shutdown-desc = Корректно завершает работу сервера.
cmd-shutdown-help = Использование: {$command}

cmd-saveconfig-desc = Сохраняет конфигурацию сервера в файл конфигурации.
cmd-saveconfig-help = Использование: {$command}

cmd-netaudit-desc = Выводит информацию о безопасности NetMsg.
cmd-netaudit-help = Использование: {$command}

# Команды игрока
cmd-tp-desc = Телепортирует игрока в любое место раунда.
cmd-tp-help = Использование: {$command} <x> <y> [<mapID>]

cmd-tpto-desc = Телепортирует текущего игрока или указанных игроков/сущностей к местоположению первого игрока/сущности.
cmd-tpto-help = Использование: {$command} <имя_пользователя|uid> [имя_пользователя|NetEntity]...
cmd-tpto-destination-hint = пункт назначения (NetEntity или имя пользователя)
cmd-tpto-victim-hint = сущность для телепортации (NetEntity или имя пользователя)
cmd-tpto-parse-error = Не удаётся определить сущность или игрока: {$str}

cmd-listplayers-desc = Выводит список всех подключённых игроков.
cmd-listplayers-help = Использование: {$command}

cmd-kick-desc = Отключает подключённого игрока от сервера.
cmd-kick-help = Использование: {$command} <индекс_игрока> [<причина>]

# Команда вращения
cmd-spin-desc = Заставляет сущность вращаться. По умолчанию — родительская сущность привязанного игрока.
cmd-spin-help = Использование: {$command} скорость [сопротивление] [entityUid]

# Команда локализации
cmd-rldloc-desc = Перезагружает локализацию (клиент и сервер).
cmd-rldloc-help = Использование: {$command}

# Отладочные команды управления сущностями
cmd-spawn-desc = Создаёт сущность указанного типа.
cmd-spawn-help = Использование: {$command} <прототип> | {$command} <прототип> <ID относительной сущности> | {$command} <прототип> <x> <y>
cmd-cspawn-desc = Создаёт клиентскую сущность указанного типа под ногами игрока.
cmd-cspawn-help = Использование: {$command} <тип_сущности>

cmd-dumpentities-desc = Сохраняет список сущностей.
cmd-dumpentities-help = Использование: {$command}
                        Сохраняет список сущностей с их UID и прототипами.

cmd-getcomponentregistration-desc = Получает информацию о регистрации компонента.
cmd-getcomponentregistration-help = Использование: {$command} <имяКомпонента>

cmd-showrays-desc = Переключает отладочную отрисовку лучей физики. Необходимо указать целое число для <времени_жизни_луча>.
cmd-showrays-help = Использование: {$command} <время_жизни_луча>

cmd-disconnect-desc = Немедленно отключается от сервера и возвращается в главное меню.
cmd-disconnect-help = Использование: {$command}

cmd-entfo-desc = Отображает подробную диагностическую информацию о сущности.
cmd-entfo-help = Использование: {$command} <entityuid>
    UID сущности может быть с префиксом 'c' для преобразования в клиентский UID.

cmd-fuck-desc = Выбрасывает исключение.
cmd-fuck-help = Использование: {$command}

cmd-showpos-desc = Показывает позиции всех сущностей на экране.
cmd-showpos-help = Использование: {$command}

cmd-showrot-desc = Показывает поворот всех сущностей на экране.
cmd-showrot-help = Использование: {$command}

cmd-showvel-desc = Показывает локальную скорость всех сущностей на экране.
cmd-showvel-help = Использование: {$command}

cmd-showangvel-desc = Показывает угловую скорость всех сущностей на экране.
cmd-showangvel-help = Использование: {$command}

cmd-sggcell-desc = Выводит список сущностей в ячейке привязочной сетки.
cmd-sggcell-help = Использование: {$command} <gridID> <vector2i>\nПараметр vector2i имеет формат x<int>,y<int>.

cmd-overrideplayername-desc = Изменяет имя, используемое при подключении к серверу.
cmd-overrideplayername-help = Использование: {$command} <имя>

cmd-showanchored-desc = Показывает закреплённые сущности на определённой плитке.
cmd-showanchored-help = Использование: {$command}

cmd-dmetamem-desc = Сохраняет список членов типа в формате, подходящем для файла конфигурации песочницы.
cmd-dmetamem-help = Использование: {$command} <тип>

cmd-launchauth-desc = Загружает токены аутентификации из данных лаунчера для тестирования на живых серверах.
cmd-launchauth-help = Использование: {$command} <имя_аккаунта>

cmd-lightbb-desc = Переключает отображение ограничивающих прямоугольников источников света.
cmd-lightbb-help = Использование: {$command}

cmd-monitorinfo-desc = Информация о мониторах.
cmd-monitorinfo-help = Использование: {$command} <id>

cmd-setmonitor-desc = Установить монитор.
cmd-setmonitor-help = Использование: {$command} <id>

cmd-physics-desc = Показывает отладочный оверлей физики. Указанный аргумент определяет тип оверлея.
cmd-physics-help = Использование: {$command} <aabbs / com / contactnormals / contactpoints / distance / joints / shapeinfo / shapes>

cmd-hardquit-desc = Мгновенно завершает работу игрового клиента.
cmd-hardquit-help = Использование: {$command}
                    Мгновенно завершает работу клиента, не оставляя следов. Не отправляет серверу сигнал отключения.

cmd-quit-desc = Корректно завершает работу игрового клиента.
cmd-quit-help = Использование: {$command}
                Корректно завершает работу клиента, уведомляя подключённый сервер и выполняя необходимые действия.

cmd-csi-desc = Открывает интерактивную консоль C#.
cmd-csi-help = Использование: {$command}

cmd-scsi-desc = Открывает интерактивную консоль C# на сервере.
cmd-scsi-help = Использование: {$command}

cmd-watch-desc = Открывает окно наблюдения за переменными.
cmd-watch-help = Использование: {$command}

cmd-showspritebb-desc = Переключает отображение границ спрайтов.
cmd-showspritebb-help = Использование: {$command}

cmd-togglelookup-desc = Показывает/скрывает границы поиска сущностей через оверлей.
cmd-togglelookup-help = Использование: {$command}

cmd-net_entityreport-desc = Переключает панель отчёта по сетевым сущностям.
cmd-net_entityreport-help = Использование: {$command}

cmd-net_refresh-desc = Запрашивает полное состояние сервера.
cmd-net_refresh-help = Использование: {$command}

cmd-net_graph-desc = Переключает панель статистики сети.
cmd-net_graph-help = Использование: {$command}

cmd-net_watchent-desc = Выводит все сетевые обновления для EntityId в консоль.
cmd-net_watchent-help = Использование: {$command} <0|EntityUid>

cmd-net_draw_interp-desc = Переключает отладочную отрисовку сетевой интерполяции.
cmd-net_draw_interp-help = Использование: {$command} <0|EntityUid>

cmd-vram-desc = Отображает статистику использования видеопамяти игрой.
cmd-vram-help = Использование: {$command}

cmd-showislands-desc = Показывает текущие физические тела, входящие в каждый физический остров.
cmd-showislands-help = Использование: {$command}

cmd-showgridnodes-desc = Показывает узлы, используемые для разделения сетки.
cmd-showgridnodes-help = Использование: {$command}

cmd-profsnap-desc = Создаёт снимок профилирования.
cmd-profsnap-help = Использование: {$command}

cmd-devwindow-desc = Окно разработчика.
cmd-devwindow-help = Использование: {$command}

cmd-scene-desc = Немедленно изменяет сцену/состояние UI.
cmd-scene-help = Использование: {$command} <имяКласса>

cmd-szr_stats-desc = Выводит статистику сериализатора.
cmd-szr_stats-help = Использование: {$command}

cmd-hwid-desc = Возвращает текущий HWID (идентификатор оборудования).
cmd-hwid-help = Использование: {$command}

cmd-vvread-desc = Получает значение по пути с помощью VV (просмотр переменных).
cmd-vvread-help = Использование: {$command} <путь>

cmd-vvwrite-desc = Изменяет значение по пути с помощью VV (просмотр переменных).
cmd-vvwrite-help = Использование: {$command} <путь>

cmd-vvinvoke-desc = Вызывает метод по пути с аргументами с помощью VV.
cmd-vvinvoke-help = Использование: {$command} <путь> [аргументы...]

cmd-dump_dependency_injectors-desc = Сохраняет кэш внедрения зависимостей IoCManager.
cmd-dump_dependency_injectors-help = Использование: {$command}
cmd-dump_dependency_injectors-total-count = Общее количество: { $total }

cmd-dump_netserializer_type_map-desc = Сохраняет карту типов и хэш сериализатора NetSerializer.
cmd-dump_netserializer_type_map-help = Использование: {$command}

cmd-hub_advertise_now-desc = Немедленно отправляет рекламу на главный хаб-сервер.
cmd-hub_advertise_now-help = Использование: {$command}

cmd-echo-desc = Выводит аргументы обратно в консоль.
cmd-echo-help = Использование: {$command} "<сообщение>"

## Команда 'vfs_ls'
cmd-vfs_ls-desc = Выводит содержимое директории в VFS.
cmd-vfs_ls-help = Использование: {$command} <путь>
    Пример:
    vfs_list /Assemblies

cmd-vfs_ls-err-args = Требуется ровно 1 аргумент.
cmd-vfs_ls-hint-path = <путь>

cmd-reloadtiletextures-desc = Перезагружает атлас текстур тайлов, позволяя горячую перезагрузку спрайтов тайлов.
cmd-reloadtiletextures-help = Использование: {$command}

cmd-audio_length-desc = Показывает длительность аудиофайла
cmd-audio_length-help = Использование: {$command} { cmd-audio_length-arg-file-name }
cmd-audio_length-arg-file-name = <имя файла>

## PVS
cmd-pvs-override-info-desc = Выводит информацию о переопределениях PVS, связанных с сущностью.
cmd-pvs-override-info-empty = У сущности {$nuid} нет переопределений PVS.
cmd-pvs-override-info-global = У сущности {$nuid} есть глобальное переопределение.
cmd-pvs-override-info-clients = У сущности {$nuid} есть переопределение сессии для {$clients}.

cmd-localization_set_culture-desc = Устанавливает DefaultCulture для LocalizationManager клиента.
cmd-localization_set_culture-help = Использование: {$command} <имя_культуры>
cmd-localization_set_culture-culture-name = <имя_культуры>
cmd-localization_set_culture-changed = Локализация изменена на { $code } ({ $nativeName } / { $englishName })

cmd-addmap-hint-2 = runMapInit [true / false]
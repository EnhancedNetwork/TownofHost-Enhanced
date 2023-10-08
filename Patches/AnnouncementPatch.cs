using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.Data;
using AmongUs.Data.Player;
using Assets.InnerNet;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace TOHE;

// ##https://github.com/Yumenopai/TownOfHost_Y
public class ModNews
{
    public int Number;
    public int BeforeNumber;
    public string Title;
    public string SubTitle;
    public string ShortTitle;
    public string Text;
    public string Date;

    public Announcement ToAnnouncement()
    {
        var result = new Announcement
        {
            Number = Number,
            Title = Title,
            SubTitle = SubTitle,
            ShortTitle = ShortTitle,
            Text = Text,
            Language = (uint)DataManager.Settings.Language.CurrentLanguage,
            Date = Date,
            Id = "ModNews"
        };

        return result;
    }
}
[HarmonyPatch]
public class ModNewsHistory
{
    public static List<ModNews> AllModNews = new();

    // When creating new news, you can not delete old news 
    public static void Init()
    {
    // ====== English ======
        if (TranslationController.Instance.currentLanguage.languageID == SupportedLangs.English)
        {
            {
                var news = new ModNews
                {
                    Number = 100002,
                    Title = "Town of Host: Enhanced v1.0.1",
                    SubTitle = "★★ A New Era ★★",
                    ShortTitle = "TOH: Enhanced v1.0.1",
                    BeforeNumber = 100001,
                    Text = "<size=150%>Welcome to TOH: Enhanced v1.0.1!</size>" +
                    "\n<b>【Base】</b>\n - Base on TOH-RE v3.1.0 (Notes Available)\r\n" +
                    "\n<b>【Hotfixes】</b>" +
                    "\n     - Removed spray of Loonie and replaced it" +
                    "\n     - Added an updated clue in Fortune Teller" +
                    "\n     - Fixed templates and VIP List not generating" +
                    "\n     - A new tease...for a new role...?" +
                    "\n\n★ Welcome to Town of Host: Enhanced! ★",

                    Date = "2023-10-5T00:00:00Z"
                };
                AllModNews.Add(news);
            }
            {
                var news = new ModNews
                {
                    Number = 100001,
                    Title = "Town of Host: Enhanced v1.0.0",
                    SubTitle = "★★ A New Era ★★",
                    ShortTitle = "TOH: Enhanced v1.0.0",
                    BeforeNumber = 100000,
                    Text = "<size=150%>Welcome to TOH: Enhanced v1.0.0!</size>" +
                    "\n<b>【Base】</b>\n - Base on TOH-RE v3.1.0 (Notes Available)\r\n" +
                    "\n<b>【Changes/Fixes】</b>" +
                    "\n     - Removed all association with Loonie, credit in README" +
                    "\n     - Renamed Jailor -> Jailer (you're welcome, ryuk)" +
                    "\n     - Updated templates with all strings/variables" +
                    "\n     - Fixed Bandit Text String" +
                    "\n\n★ Made the mod better. Welcome to Town of Host: Enhanced! ★",

                    Date = "2023-10-5T00:00:00Z"
                };
                AllModNews.Add(news);
            }
            {
                // When making new changes/roles, add information
                // TOHE v3.1.0
                var news = new ModNews
                {
                    Number = 100000,
                    Title = "Town of Host Re-Edited v3.1.0",
                    SubTitle = "★★Smaller update, but still kinda large★★",
                    ShortTitle = "★TOH-RE v3.1.0",
                    Text = "<size=150%>Welcome to TOHE v3.1.0!</size>\n\n<size=125%>Support for Among Us v2023.7.11 and v2023.7.12</size>\n"

                        + "\n<b><size=180%>Now this version supports public lobbies!</size></b>\r\n"

                        + "\n<b>【Base】</b>\n - Base on TOH v4.1.2\r\n"

                        + "\n<b>【New Roles】</b>" +
                        "\n\r<b><i>Impostor: (5 roles)</i></b>" +
                            "\n     - Evil Mini" +
                            "\n     - Mastermind" +
                            "\n     - Vampiress (from TOH-TOR and it's hidden role)" +
                            "\n     - Blackmailer (experimental)" +
                            "\n     - Undertaker\n\r" +

                        "\n\r<b><i>Crewmate: (4 roles)</i></b>" +
                            "\n     - Nice Mini" +
                            "\n     - Bastion" +
                            "\n     - Alchemist" +
                            "\n     - Investigator (experimental)\n\r" +

                        "\n\r<b><i>Neutral: (5 roles)</i></b>" +
                            "\n     - Bandit" +
                            "\n     - Imitator" +
                            "\n     - Pyromaniac" +
                            "\n     - Huntsman" +
                            "\n     - Doppelganger (experimental)\n\r" +

                        "\n\r<b><i>Add-on: (5 add-ons)</i></b>" +
                            "\n     - Cyber" +
                            "\n     - Bloodlust" +
                            "\n     - Circumvent" +
                            "\n     - Stubborn" +
                            "\n     - Overclocked\n\r" +

                        "\n\r<b>【Bug Fixes】</b>" +
                            "\n     - Fixed Mad Psychic" +
                            "\n     - Fixed Werewolf (no longer leaves duplicate bodies)" +
                            "\n     - Fixed Swapper kicking clients" +
                            "\n     - Fixed Setting «Disable Close Door»" +
                            "\n     - Fixed Egoist win screen showing double" +
                            "\n     - Fixed minor issue with Eraser" +
                            "\n     - Fixed Infectious and Shroud" +
                            "\n     - Fixed «Neutrals Win Together» (Again)" +
                            "\n     - Fixed bug when Lawyer and Executioner have tasks" +
                            "\n     - Fixed Hex Master and Wraith win condition" +
                            "\n     - Fixed bug when Necroview and Visionary used the ability when they are dead" +
                            "\n     - Fixed bug that caused some roles can not guess add-ons under certain conditions if the «Can Guess Add-Ons» setting is enabled\n\r" +

                        "\n\r<b>【Rework Roles】</b>" +
                            "\n     - Necromancer" +
                            "\n     - Arsonist" +
                            "\n     - Glitch\n\r" +

                        "\n\r<b>【Improvements Roles】</b>" +
                            "\n     - Tracker" +
                            "\n     - Amnesiac" +
                            "\n     - Puppeteer\n\r" +

                        "\n\r<b>【New Mod Settings】</b>" +
                            "\n     - Setting: Random Spawns On Vents\n\r" +

                        "\n\r<b>【New Features】</b>" +
                            "\n     - Added VIP system\n\r" +

                        "\n\r<b>【Some Changes】</b>" +
                            "\n     - Keybind «Set Default All TOHE Options» was returned (Ctrl + Delete)" +
                            "\n     - Demon is now Neutral Killer (was Neutral Evil)" +
                            "\n     - Bomber, Nuker and Medic can no longer be Fragile" +
                            "\n     - Some neutral roles have been given the «Can Vent» and «Has Impostor Vision» setting" +
                            "\n     - Judge is now counted as Crewmate Killing" +
                            "\n     - When certain roles abilities end, their votes will no longer be hidden if the «Hide Vote» setting is enabled (Divinator, Eraser, Tracker, Oracle)" +
                            "\n     - Added warning for when black screen prevention is disabled but the NK count is higher than 1" +
                            "\n     - The Bounty Hunter target's nickname is now shown in dark red" +
                            "\n     - Trickster no longer counts towards the Impostor remaining count for the /kcount command and ejection scene\n\r" +

                        "\n\r<b>【Removals】</b>" +
                            "\n     - Removed Covens (Poisoner, Jinx, Potion Master, Medusa, Wraith, and Hex Master are now neutral killers again.\nShade, Coven Leader, and Ritualist are now unused)",

                    Date = "2023-9-29T00:00:00Z"

                };
                AllModNews.Add(news);
            }
        }
    // ====== Russian ======
        else if (TranslationController.Instance.currentLanguage.languageID == SupportedLangs.Russian)
        {
            {
                // TOHE v3.1.0
                var news = new ModNews
                {
                    Number = 90001,
                    Title = "Town of Host Re-Edited v3.1.0",
                    SubTitle = "★★Следующее небольшое обновление★★",
                    ShortTitle = "★TOH-RE v3.1.0",
                    Text = "<size=150%>Добро Пожаловать в TOHE v3.1.0!</size>\n\n<size=125%>Поддерживает версию Among Us v2023.7.11 и v2023.7.12</size>\n"

                        + "\n<b><size=180%>Теперь эта версия поддерживает публичные лобби!</size></b>\r\n"

                        + "\n<b>【Основан】</b>\n - Основан на TOH v4.1.2\r\n"

                        + "\n<b>【Новые роли】</b>" +
                        "\n\r<b><i>Предатель: (4 роли)</i></b>" +
                            "\n     - Злой Мини" +
                            "\n     - Вампирша (Из TOH-TOR и это скрытая роль)" +
                            "\n     - Шантажист (Эксперементальная роль)" +
                            "\n     - Андертейкер\n\r" +

                        "\n\r<b><i>Член Экипажа: (4 роли)</i></b>" +
                            "\n     - Добрый Мини" +
                            "\n     - Бастион" +
                            "\n     - Алхимик" +
                            "\n     - Исследователь\n\r" +

                        "\n\r<b><i>Нейтрал: (5 ролей)</i></b>" +
                            "\n     - Бандит" +
                            "\n     - Имитатор" +
                            "\n     - Пиромант" +
                            "\n     - Охотник" +
                            "\n     - Двойник (Эксперементальная роль)\n\r" +

                        "\n\r<b><i>Атрибут: (5 атрибута)</i></b>" +
                            "\n     - Знаменитый" +
                            "\n     - Кровожадный" +
                            "\n     - Расстройчивый" +
                            "\n     - Упрямый" +
                            "\n     - Разогнанный\n\r" +

                        "\n\r<b>【Исправление Багов】</b>" +
                            "\n     - Исправлен Безумный Экстрасенс" +
                            "\n     - Исправлен Оборотень (больше не оставляет двойные трупы)" +
                            "\n     - Исправлен баг когда Обменник кикал игроков" +
                            "\n     - Исправлена настройка «Отключить саботаж дверей»" +
                            "\n     - Исправлен экран победы Эгоиста который показывал двух победителей" +
                            "\n     - Исправлены некоторые баги со Стирачкой" +
                            "\n     - Исправлены некоторые баги у Заразного и у Накрывателя" +
                            "\n     - Исправлена настройка «Все Нейтралы побеждают вместеr» (Снова)" +
                            "\n     - Исправлен баг когда у Адвоката и Палача были задания" +
                            "\n     - Исправлено условие победы у Мастера Проклятий и у Духа" +
                            "\n     - Исправлен баг когда Некровил и Визионер использовали способность, когда они были мертвы" +
                            "\n     - Исправлен баг из-за этого некоторые роли не могли угадать атрибуты при определенных условиях, если включена настройка «Может угадывать атрибуты»\n\r" +

                        "\n\r<b>【Переработка Ролей】</b>" +
                            "\n     - Некромант" +
                            "\n     - Арзонист" +
                            "\n     - Глич\n\r" +

                        "\n\r<b>【Улучшения Ролей】</b>" +
                            "\n     - Трекер" +
                            "\n     - Амнезияк" +
                            "\n     - Кукловод\n\r" +

                        "\n\r<b>【Новые Настройки Мода】</b>" +
                            "\n     - Настройка: Случайные появления на вентиляциях\n\r" +

                        "\n\r<b>【Новые Функции】</b>" +
                            "\n     - Добавлена VIP система\n\r" +

                        "\n\r<b>【Некоторые Изменения】</b>" +
                            "\n     - Система модераторов теперь полностью переведена и не содержит пустых сообщений" +
                            "\n     - Привязка клавиш «Установить все настройки TOHE по умолчанию» был возвращен (Ctrl + Delete)" +
                            "\n     - Демон теперь Нейтральный Убийца (был Злым Нейтралом)" +
                            "\n     - Бомбер, Крипер и Медик больше не могут стать Хрупким (Атрибут)" +
                            "\n     - Некоторым нейтральным ролям были добавлены настройки «Может использовать вентиляцию» и «Имеет дальность обзора Предателя»" +
                            "\n     - Судья теперь считается как Член Экипажа Убийца" +
                            "\n     - Когда навыки у Следователя, Стирачки, Трекера или у Оракла заканчивались, их голоса больше не будут скрываться, если у них включена настройка «Скрыть голос»" +
                            "\n     - Добавлено предупреждение, когда настройка «Предотвращение черного экрана» отключена, но количество Нейтральных Убийц в игре больше одного" +
                            "\n     - Никнейм цели Охотника за Головами теперь отображается темно-красным" +
                            "\n     - Трюкач больше не учитывается при подсчете оставшихся Предателей при использовании /kcount и во время сцены изгнания\n\r" +

                        "\n\r<b>【Уделаены】</b>" +
                            "\n     - Удалена команда Ковенов (Отравитель, Джинкс, Ритуальщик, Медуза, Дух и Мастер Проклятий теперь снова Нейтральные Убийцы.\nТень, Лидер Ковена и Фокусник теперь не используются)",

                    Date = "2023-9-29T00:00:00Z"

                };
                AllModNews.Add(news);
            }
            {
                // TOHE v3.0.0
                var news = new ModNews
                {
                    Number = 90000,
                    Title = "Town of Host Re-Edited v3.0.0",
                    SubTitle = "★★Следующее большое обновление!★★",
                    ShortTitle = "★TOH-RE v3.0.0",
                    Text = "<size=150%>Добро Пожаловать в TOHE v3.0.0!</size>\n\n<size=125%>Поддерживает версию Among Us v2023.7.11 и v2023.7.12</size>\n"

                        + "\n<b>【Основан】</b>\n - Основан на TOH v4.1.2\r\n"

                        + "\n<b>【Новые роли】</b>" +
                        "\n\r<b><i>Предатель: (5 ролей)</i></b>" +
                            "\n     - Крипер (скрытый)" +
                            "\n     - Ловушка" +
                            "\n     - Крестный" +
                            "\n     - Людопат" +
                            "\n     - Берсерк\n\r" +

                        "\n\r<b><i>Член Экипажа: (13 ролей)</i></b>" +
                            "\n     - Поклонник" +
                            "\n     - Подражатель" +
                            "\n     - Повелитель Времени" +
                            "\n     - Крестоносец" +
                            "\n     - Мечтатель" +
                            "\n     - Дозорный" +
                            "\n     - Коммуникатор" +
                            "\n     - Хамелеон" +
                            "\n     - Очиститель" +
                            "\n     - Зажигалка" +
                            "\n     - Мастер Задач" +
                            "\n     - Тюремщик" +
                            "\n     - Обменник (Эксперементальная роль)\n\r" +

                        "\n\r<b><i>Нейтрал: (17 ролей)</i></b>" +
                            "\n     - Амнезияк" +
                            "\n     - Носитель Чумы/Чума" +
                            "\n     - Мазохист" +
                            "\n     - Предсказатель" +
                            "\n     - Пират" +
                            "\n     - Накрыватель" +
                            "\n     - Оборотень" +
                            "\n     - Шаман" +
                            "\n     - Окультист" +
                            "\n     - Романтик (Мстительный Романтик & Безжалостный Романтик)" +
                            "\n     - Тень" +
                            "\n     - Ищущий" +
                            "\n     - Агитатор" +
                            "\n     - Коллектор Душ\n\r" +

                        "\n\r<b><i>Добавлена ​​новая фракция: Ковен: (10 ролей)</i></b>" +
                            "\n     - Банши" +
                            "\n     - Лидер Ковена" +
                            "\n     - Некромант" +
                            "\n     - Ритуальщик" +
                            "\n     - Джинкс теперь роль Ковена" +
                            "\n     - Мастер Проклятий теперь роль Ковена" +
                            "\n     - Medusa теперь роль Ковена" +
                            "\n     - Отравитель теперь роль Ковена" +
                            "\n     - Фокусник теперь роль Ковена" +
                            "\n     - Дух теперь роль Ковена\n\r" +

                        "\n\r<b><i>Атрибут: (12 атрибутов)</i></b>" +
                            "\n     - Гуль" +
                            "\n     - Неудачный" +
                            "\n     - Забывчивый (возвращён)" +
                            "\n     - Мученик" +
                            "\n     - Антидот" +
                            "\n     - Взрывной" +
                            "\n     - Неуклюжий" +
                            "\n     - Сыщик" +
                            "\n     - Внимательный" +
                            "\n     - Пустой" +
                            "\n     - Механик" +
                            "\n     - Хрупкий\n\r" +

                        "\n\r<b>【Переработка Ролей/Атрибутов】</b>" +
                            "\n     - Бомбер" +
                            "\n     - Медик" +
                            "\n     - Шакал" +
                            "\n     - Ловец" +
                            "\n     - Ночной (теперь это атрибут)\n\r" +

                        "\n\r<b>【Исправление Багов】</b>" +
                            "\n     - Исправлен Фонарик" +
                            "\n     - Исправлена ​​фатальная ошибка у Крестоносца" +
                            "\n     - Исправлено долгое время загрузки игры с модом" +
                            "\n     - Джинкс больше не сможет каким-то образом сглазить себя" +
                            "\n     - Проклятый волк больше не сможет каким-либо образом проклинать себя" +
                            "\n     - Исправлена ​​ошибка, когда в настройках мода появлялся дополнительный заголовок" +
                            "\n     - Исправлена ​​ошибка, когда не-хост игрок с модом не мог угадать Атрибуты и некоторые роли" +
                            "\n     - Исправлена ​​ошибка, когда некоторые роли не могли получить Атрибуты" +
                            "\n     - Исправлена ​​ошибка, из-за которой игроки, не являющиеся Любовниками, умирали вместе" +
                            "\n     - Исправлена ​​ошибка, из-за которой щит в лобби появлялся из-за Стервятника" +
                            "\n     - Исправлена ​​ошибка, из-за которой у роли Искателя иногда не было стрелок" +
                            "\n     - Исправлена ​​ошибка, из-за которой настройка «Нейтралы побеждают вместе» не работала." +
                            "\n     - Исправлена ​​ошибка, когда некоторые нейтральные роли не могли нажать кнопку саботажа (Хост)." +
                            "\n     - Исправлена ​​ошибка, когда Кукловод и Заклинатель не могли видеть марку у цели." +
                            "\n     - Исправлен сломанный Зум" +
                            "\n     - Некоторые исправления черного экрана (И некоторая защита)" +
                            "\n     - Некоторые исправления у Шерифа" +
                            "\n     - Исправлены стрелки у Трекера" +
                            "\n     - Исправлен Следователь" +
                            "\n     - Исправлена ​​ошибка, когда текст «Поглощен» не появлялся у игрока" +
                            "\n     - Исправлены некоторые конфликты у Атрибутов" +
                            "\n     - Исправлен значок кнопки репорта у Хоста\n\r" +

                        "\n\r<b>【Улучшения Ролей】</b>" +
                            "\n     - Следователь" +
                            "\n     - Маньяк" +
                            "\n     - Камуфляжер" +
                            "\n     - Возмездник сбалансирован" +
                            "\n     - Настройка: Поджигатель продолжает игру" +
                            "\n     - Настройка у Вектора: Откат вентиляции" +
                            "\n     - Настройка у Шута: Имеет дальность обзора Предателя" +
                            "\n     - Настройка у Мстителя: Члены Экипажа/Нейтралы могут стать Мстителем" +
                            "\n     - Настройка у Судьи: Может судить Ковенов" +
                            "\n     - Настройка: Обзор Фонарика меняется при саботаже света" +
                            "\n     - Добавлена настройка продолжительности морфа у Шахтера и Баглаеца" +
                            "\n     - Добавлен лимит способности у Повелителя Времени и Гренадёр" +
                            "\n     - Добавлена ​​возможность повышения способностей при выполнении заданий для: Коронер, Хамелеон, Трекер, Ремонтник, Оракл, Инспектор, Медиум, Следователь, Гренадёр, Ветеран, Повелитель Времени и Пацифист" +
                            "\n     - Настройка позволяющая скрыть лимит выстрелов у Шерифа" +
                            "\n     - Настройка для Следователь, показывает ли он конкретные роли вместо подсказок после завершения заданий" +
                            "\n     - Настройка для Искателя, определяющие задержку появления стрелок" +
                            "\n     - Настройка для Гробовщика, может иметь стрелку которая введёт к труам" +
                            "\n     - Настройка Оракла, определяющая вероятность отображения неверного результата" +
                            "\n     - Настройки для Ремонтника позволет отнять количество способности при починке саботажа Реактора/O2 и Свет/Связь" +
                            "\n     - Настройка для Невидимки, Wraith, Хамелеона и Shade которая позволяет прыгать в вентиляцию когда невидимость находится в откате" +
                            "\n     - Настройка: Байт может быть зарепорчен при любых условиях" +
                            "\n     - Хамелеон использует откат вентиляции инженера" +
                            "\n     - Откат Вампира и Отравителя теперь сбрасывается, когда укушенный/отравленный игрок умирает\n\r" +

                        "\n\r<b>【Новые Клиентские Настройки】</b>" +
                            "\n     - Показывать FPS" +
                            "\n     - Мастер Игры (GM) (был перемещён)" +
                            "\n     - Наложение Текста (Показывать текст например как: Игра не закончится, Низкая Нагрузка и т.д.)" +
                            "\n     - Режим Маленького Экрана" +
                            "\n     - Старый Результат Игры (По умолчанию используется новый)\n\r" +

                        "\n\r<b>【Новые Настройки Мода】</b>" +
                            "\n     - Использовать защиту от чёрных экранов" +
                            "\n     - Включить использование команды /kcount" +
                            "\n     - Видеть роли изганных во время встречи" +
                            "\n     - Убрать питомцев у мёртвых игроков (Борьба с ванильным багом)" +
                            "\n     - Отключить ненужные анимации щитов" +
                            "\n     - Отключить анимацию убийств во время угадывания" +
                            "\n     - Отключить камуфляж на некоторых картах" +
                            "\n     - Блокировать переключатели когда они подняты" +
                            "\n     - Изменить откат саботажа" +
                            "\n     - Сбросить двери после встречи (Airship/Polus)\n\r" +

                        "\n\r<b>【Некоторые Изменения】</b>" +
                            "\n     - Текст победы и поражения больше не окрашивается от ролей" +
                            "\n     - Последнего Предатлея (Атрибут) больше невозможно угадать" +
                            "\n     - Задания от Членов Экипажа Любовников теперь засчитываются в счет победы по заданиям" +
                            "\n     - Зараженные игроки теперь умирают после встречи, если в живых нет Заразного" +
                            "\n     - Репорт трупа во время камуфляжа теперь разделены" +
                            "\n     - Ловец, Вектор, Эгоист, Революционист, Провокатор, Угадыватель больше не являются эксперементальными ролями" +
                            "\n     - Добавлена ​​возможность менять настройки на 5 вместо 1 при удержании Левого/Правого Shift." +
                            "\n     - Откат всех способностей теперь сбрасывается после встреч" +
                            "\n     - Любовники больше не могут стать Солнечным Мальчиком" +
                            "\n     - Панель задач теперь всегда отключена" +
                            "\n     - Вешатель перемещён в экспериментальные роли из-за багов" +
                            "\n     - Роли которые имеют те же способности что и Атрибуты не будут появляться, если эти Атрибуты включены" +
                            "\n     - Команда \"/r\" была улучшена\n\r" +

                        "\n\r<b>【Новые Функции】</b>" +
                            "\n     - Время загрузки мода теперь значительно ускорилась" +
                            "\n     - Мод переведён на Испанский (частично)" +
                            "\n     - Мод переведён на Китайский" +
                            "\n     - Улучшена настройка случайной карты" +
                            "\n     - Главное меню было изменено" +
                            "\n     - Добавлены новые кнопки в главном меню" +
                            "\n     - Добавлены функции для автоматического запуска" +
                            "\n     - Переработанн статус активности игры в профиле Дискорда" +
                            "\n     - Улучшение модератора и спонсора (/kick, /ban, /warn, и теги модератора)" +
                            "\n     - Файл шаблона по умолчанию был обновлен и улучшен" +
                            "\n     - Переработанн результат игры (В настройках клиента также можно вернуть старую)" +
                            "\n     - Улучшена настройка позволяющая кикать игроков играющих на дргуих платформах" +
                            "\n     - Добавлена проверка поддерживаемой версии Among Us\n\r" +

                            "\n\r<b>【Уделаены】</b>" +
                            "\n     - Удалён Режим ПВП" +
                            "\n     - Удалён атрибут Нептуна" +
                            "\n     - Удалена роль Капиталиста\n\r" +

                    "\n**Возможно указаны не все изменения, так как мог что-то упустить из виду**",

                    Date = "2023-9-16T00:00:00Z"

                };
                AllModNews.Add(news);
            }
        }
    // ====== SChinese ======
        else if (TranslationController.Instance.currentLanguage.languageID == SupportedLangs.SChinese)
        {
            {
                // TOHE v1.0.0
                var news = new ModNews
                {
                    Number = 80002,
                    Title = "Town of Host: Enhanced v1.0.0",
                    SubTitle = "★★ 新时代 ★★",
                    ShortTitle = "TOH: Enhanced v1.0.0",
                    BeforeNumber = 80001,
                    Text = "<size=150%>欢迎来到 TOH: Enhanced v1.0.0!</size>" +
                    "\n<b>【基于官方版本】</b>\n - 基于 TOH-RE v3.1.0 (Notes Available)\r\n" +
                    "\n<b>【更改/修复】</b>" +
                    "\n     - 删除了与 Lounie 的所有关联，自述文件中的信用" +
                    "\n     - 改名了 Jailor -> Jailer (不用客气, ryuk)" +
                    "\n     - 更新了包含所有string/variable" +
                    "\n     - 修复了强盗文本文字" +
                    "\n\n★ 使模组变得更好。欢迎来到 Town of Host: Enhanced！ ★",

                    Date = "2023-10-5T00:00:00Z"
                };
                AllModNews.Add(news);
            }
        }
    }

    [HarmonyPatch(typeof(PlayerAnnouncementData), nameof(PlayerAnnouncementData.SetAnnouncements)), HarmonyPrefix]
    public static bool SetModAnnouncements(PlayerAnnouncementData __instance, [HarmonyArgument(0)] ref Il2CppReferenceArray<Announcement> aRange)
    {
        if (!AllModNews.Any())
        {
            Init();
            AllModNews.Sort((a1, a2) => { return DateTime.Compare(DateTime.Parse(a2.Date), DateTime.Parse(a1.Date)); });
        }

        List<Announcement> FinalAllNews = new();
        AllModNews.Do(n => FinalAllNews.Add(n.ToAnnouncement()));
        foreach (var news in aRange)
        {
            if (!AllModNews.Any(x => x.Number == news.Number))
                FinalAllNews.Add(news);
        }
        FinalAllNews.Sort((a1, a2) => { return DateTime.Compare(DateTime.Parse(a2.Date), DateTime.Parse(a1.Date)); });

        aRange = new(FinalAllNews.Count);
        for (int i = 0; i < FinalAllNews.Count; i++)
            aRange[i] = FinalAllNews[i];

        return true;
    }
}
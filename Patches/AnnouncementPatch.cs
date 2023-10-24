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
                    Number = 100004,
                    Title = "Town of Host: Enhanced v1.1.1",
                    SubTitle = "★★ A performance update with bug fixes! ★★",
                    ShortTitle = "TOH: Enhanced v1.1.1",
                    BeforeNumber = 100003,
                    Text = "<size=150%>Welcome to TOH: Enhanced v1.1.1!</size>\n" +
                    "\n<b>【Base】</b>\n - Base on TOH: Enhanced v1.1.0\r\n" +

                    "\n<b>【Bug Fixes】</b>" +
                        "\n     - Fixed bugs with (Evil) Mini not being judgeable or revenged" +
                        "\n     - Fixed Swapper and several bugs within it, staying experimental" +
                        "\n     - Fixed Berserker not able to die and spamming errors" +
                        "\n     - Fixed Mad Nice Mini issues" +
                        "\n     - Fixed a conflict with Tiebreaker and Void Ballot\n\r" +

                    "\n<b>【Other Fixes】</b>" +
                        "\n     - Improved overkiller in PlayerControls" +
                        "\n     - Reworked End Game Checks (again) to optimize it" +

                    "\n\n★ Welcome to Town of Host: Enhanced! ★",

                    Date = "2023-10-23T00:00:00Z"
                };
                AllModNews.Add(news);
            }
            {
                var news = new ModNews
                {
                    Number = 100003,
                    Title = "Town of Host: Enhanced v1.1.0",
                    SubTitle = "★★ An update already?! Wow! ★★",
                    ShortTitle = "TOH: Enhanced v1.1.0",
                    BeforeNumber = 100002,
                    Text = "<size=150%>Welcome to TOH: Enhanced v1.1.0!</size>\n" +
                    "\n<b>【Base】</b>\n - Base on TOH: Enhanced v1.0.1\r\n" +

                    "\n<b>【New Roles/Addons】</b>" +
                        "\n     - President (Crewmate: Power)" +
                        "\n     - Spy (Crewmate: Support)" +
                        "\n     - Vigilante (Crewmate: Killing)\n\r" +

                        "\n     - Rebound (Addons: Mixed)\n\r" +

                    "\n<b>【New Settings】</b>" +
                        "\n     - Crewpostor: Lunge on kill AND Kill after each x tasks complete" +
                        "\n     - Deathpact: Players in deathpact can call a meeting" +
                        "\n     - Twister: Hide who players swap with\n" +

                        "\n     - Deceiver: Loses ability usage on wrongful deceive" +
                        "\n     - Merchant: Can only sell enabled addons" +
                        "\n     - Coroner: Inform killer about being tracked\n\r" +

                        "\n     - Infectious: Double Click to Kill/Infect" +
                        
                        "\n     - Bewilder: Killer can get bewilder's vision\n\r" +

                    "\n<b>【Removed Roles/Addons】</b>" +
                        "\n     - Neutral: Occultist\n\r" +
                        "\n     - Addon: Sunglasses" +
                        "\n     - Addon: Glow" +

                    "\n<b>【Other Changes】</b>" +
                        "\n     - New Language Support: Portugese" +
                        "\n     - New Lobby Games: /rps & /coinflip" +
                        "\n     - Renamed Agent BACK to Evil Tracker" +
                        "\n     - Renamed Disruptor BACK to Anti Adminer" +
                        "\n     - New Camouflage Skins" +
                        "\n     - Added Default_Template.txt" +
                        "\n     - Reworked Reverie AND Hater" +

                    "\nSeveral Other Bug Changes (and when I say several, I mean SEVERAL)" +

                    "\n\n★ Welcome to Town of Host: Enhanced! ★",

                    Date = "2023-10-21T00:00:00Z"
                };
                AllModNews.Add(news);
            }
            {
                var news = new ModNews
                {
                    Number = 100002,
                    Title = "Town of Host: Enhanced v1.0.1",
                    SubTitle = "★★ A New Era ★★",
                    ShortTitle = "TOH: Enhanced v1.0.1",
                    BeforeNumber = 100001,
                    Text = "<size=150%>Welcome to TOH: Enhanced v1.0.1!</size>\n\n<size=125%>Support for Among Us v2023.7.11 and v2023.7.12</size>\n" +
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
                    Text = "<size=150%>Welcome to TOH: Enhanced v1.0.0!</size>\n\n<size=125%>Support for Among Us v2023.7.11 and v2023.7.12</size>\n" +
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
                var news = new ModNews
                {
                    Number = 90002,
                    Title = "Town of Host: Enhanced v1.0.1",
                    SubTitle = "★★ Новая Эра ★★",
                    ShortTitle = "TOH: Enhanced v1.0.1",
                    BeforeNumber = 90001,
                    Text = "<size=150%>Добро Пожаловать в TOH: Enhanced v1.0.1!</size>\n\n<size=125%>Поддерживает версию Among Us v2023.7.11 и v2023.7.12</size>\n" +
                    "\n<b>【Основан】</b>\n - Основан на TOH-RE v3.1.0\r\n" +
                    "\n<b>【Исправления】</b>" +
                    "\n     - Убран спрей Loonie и заменен на новое" +
                    "\n     - Добавлена ​​обновленная подсказка у Следователя" +
                    "\n     - Исправлен баг когда template и VIP-список не генерировались" +
                    "\n\n★ Добро Пожаловать в Town of Host: Enhanced! ★",

                    Date = "2023-10-5T00:00:00Z"
                };
                AllModNews.Add(news);
            }
            {
                var news = new ModNews
                {
                    Number = 90001,
                    Title = "Town of Host: Enhanced v1.0.0",
                    SubTitle = "★★ Новая Эра ★★",
                    ShortTitle = "TOH: Enhanced v1.0.0",
                    BeforeNumber = 90000,
                    Text = "<size=150%>Добро Пожаловать в TOH: Enhanced v1.0.0!</size>\n\n<size=125%>Поддерживает версию Among Us v2023.7.11 и v2023.7.12</size>\n" +
                    "\n<b>【Основан】</b>\n - Основан на TOH-RE v3.1.0\r\n" +
                    "\n<b>【Изменения/Исправления】</b>" +
                    "\n     - Удалены все ассоциации с Loonie, ссылки в README" +
                    "\n     - Обновлен template" +
                    "\n     - Исправлена ​​текстовая строка Бандита" +
                    "\n\n★ Сделал мод лучше. Добро пожаловать в Town of Host: Enhanced! ★",

                    Date = "2023-10-5T00:00:00Z"
                };
                AllModNews.Add(news);
            }
            {
                // TOHE v3.1.0
                var news = new ModNews
                {
                    Number = 90000,
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
        }
    // ====== SChinese ======
        else if (TranslationController.Instance.currentLanguage.languageID == SupportedLangs.SChinese)
        {
            {
                //TOHE v1.1.1
                var news = new ModNews
                {
                    Number = 80007,
                    Title = "Town of Host: Enhanced v1.1.1",
                    SubTitle = "★★ 性能更新和bug修复！★★",
                    ShortTitle = "TOH: Enhanced v1.1.1",
                    BeforeNumber =80006,
                    Text = "<size=150%>欢迎来到TOH: Enhanced v1.1.1!</size>\n" +
                    "\n<b>【基于】</b>\n - 基于TOH: Enhanced v1.1.0\r\n" +

                    "\n<b>【Bug修复】</b>" +
                        "\n     - 修复了（坏）迷你船员不可审判或复仇的bug" +
                        "\n     - 修复了换票师和其中的几个bug,保持实验性" +
                        "\n     - 修复了狂战士无法死亡和发送垃圾信息的bug" +
                        "\n     - 修复了背叛的好迷你船员的bug" +
                        "\n     - 修复了一个与破平者和无效投票的冲突bug\n\r" +

                    "\n<b>【其它修复】</b>" +
                        "\n     - 改进了PlayerControls中的overkiller功能" +
                        "\n     - 重做游戏结束时的检查（再次），以优化它" +

                    "\n\n★ 欢迎来到Town of Host: Enhanced! ★",

                    Date = "2023-10-23T00:00:00Z"
                };
                AllModNews.Add(news);
            }
            {
                //TOHE v1.1.0
                var news = new ModNews
                {
                    Number = 80005,
                    Title = "Town of Host: Enhanced v1.1.0",
                    SubTitle = "★★ 已经更新了？哇哦！ ★★",
                    ShortTitle = "TOH: Enhanced v1.1.0",
                    BeforeNumber = 80004,
                    Text = "<size=150%>欢迎来到TOH: Enhanced v1.1.0!</size>\n" +
                    "\n<b>【基于】</b>\n - 基于TOH: Enhanced v1.0.1\r\n" +

                    "\n<b>【新职业/附加职业】</b>" +
                        "\n     - 总统(船员阵营:权力)" +
                        "\n     - 间谍(船员阵营:支援)" +
                        "\n     - 义务警长(船员阵营:击杀)\n\r" +

                        "\n     - 回弹者(附加职业:混合)\n\r" +

                    "\n<b>【新设置】</b>" +
                        "\n     - 船鬼:每完成x(用字母x表示设定的任务数)项任务后，立即瞬移并击杀" +
                        "\n     - 死亡契约:处于死亡契约中的玩家可以召开紧急会议" +
                        "\n     - 龙卷风:隐藏玩家的交换对象\n" +

                        "\n     - 赝品商:失去使用错误赝品的技能" +
                        "\n     - 商人:只能出售已启用的附加职业" +
                        "\n     - 验尸官:告知带刀玩家已被追踪\n\r" +

                        "\n     - 感染者:双击即可击杀/感染" +
                        
                        "\n     - 迷幻者:带刀玩家可以获得迷幻者的视野\n\r" +

                    "\n<b>【已删除的职业/附加职业】</b>" +
                        "\n     - 中立阵营:神秘主义者\n\r" +
                        "\n     - 附加职业:眩晕者" +
                        "\n     - 附加职业:光辉" +

                    "\n<b>【其他变化】</b>" +
                        "\n     - 支持新语言:葡萄牙语" +
                        "\n     - 新大厅游戏：/rps &抛硬币" +
                        "\n     - 将特工更名为邪恶追踪者（仅限英文版本）" +
                        "\n     - 将破坏者重新命名为反对管理员（仅限英文版本）" +
                        "\n     - 新迷彩皮肤" +
                        "\n     - 添加了Default_Template.txt" +
                        "\n     - 重做遐想者和FFF团" +

                    "\n其他一些Bug更改(当我说几个时,我指的是几个)" +

                    "\n\n★ 欢迎来到Town of Host: Enhanced! ★",

                    Date = "2023-10-21T00:00:00Z"
                };
                AllModNews.Add(news);
            }
            {
                //TOHE v1.0.1
                var news = new ModNews
                {
                    Number = 80003,
                    Title = "Town of Host: Enhanced v1.0.1",
                    SubTitle = "★★ 新时代 ★★",
                    ShortTitle = "TOH: Enhanced v1.0.1",
                    BeforeNumber = 80002,
                    Text = "<size=150%>欢迎来到 TOH: Enhanced v1.0.1!</size>\n\n<size=125%>适配 Among Us v2023.7.11 和 v2023.7.12</size>\n" +
                    "\n<b>【基于官方版本】</b>\n - 基于 TOH-RE v3.1.0 (备注可用)\r\n" +
                    "\n<b>【修复】</b>" +
                    "\n     - 移除Loonie的logo，并将其取代" +
                    "\n     - 在调查员中添加了一条更新的线索" +
                    "\n     - 修复了无法生成 模板文件 和 VIP 列表的问题" +
                    "\n     - 新角色......的新戏弄？" +
                    "\n\n★ 欢迎来到 Town of Host: Enhanced! ★",

                    Date = "2023-10-5T00:00:00Z"
                };
                AllModNews.Add(news);
            }
            {
                // TOHE v1.0.0
                var news = new ModNews
                {
                    Number = 80001,
                    Title = "Town of Host: Enhanced v1.0.0",
                    SubTitle = "★★ 新时代 ★★",
                    ShortTitle = "TOH: Enhanced v1.0.0",
                    BeforeNumber = 80000,
                    Text = "<size=150%>欢迎来到 TOH: Enhanced v1.0.0!</size>" +
                    "\n<b>【基于官方版本】</b>\n - 基于 TOH-RE v3.1.0 (备注可用)\r\n" +
                    "\n<b>【更改/修复】</b>" +
                    "\n     - 删除了与 Loonie 的所有关联，自述文件中的信用" +
                    "\n     - 更新了包含所有string/variable" +
                    "\n     - 修复了强盗文本文字" +
                    "\n\n★ 使模组变得更好。欢迎来到 Town of Host: Enhanced！ ★",

                    Date = "2023-10-5T00:00:00Z"
                };
                AllModNews.Add(news);
            }
        }
           // ====== TChinese ======
        else if (TranslationController.Instance.currentLanguage.languageID == SupportedLangs.TChinese)
        {
             {
                // TOHE v1.0.1
                var news = new ModNews
                {
                    Number = 70004,
                    Title = "Town of Host: Enhanced v1.0.1",
                    SubTitle = "★★ 新時代 ★★",
                    ShortTitle = "TOH: Enhanced v1.0.1",
                    BeforeNumber = 70003,
                    Text = "<size=150%>歡迎來到 TOH: Enhanced v1.0.1!</size>\n\n<size=125%>支援版本 Among Us v2023.7.11、v2023.7.12</size>\n" +
                    "\n<b>【基於版本】</b>\n - 基於TOH-RE v3.1.0 (備註: 目前可以使用)\r\n" +
                    "\n<b>【修復】</b>" +
                    "\n     - 刪除了大廳中的Loonie標誌，並將其更換" +
                    "\n     - 在占卜師中增加了一條更新的線索" +
                    "\n     - 修復了模板以及VIP清單沒有產生的問題" +
                    "\n     - 一個新職業的預告...?" +
                    "\n\n★ 歡迎來到 Town of Host: Enhanced! ★",
                    Date = "2023-10-15T00:00:00Z",
                };
                AllModNews.Add(news);
            }
            {
                // TOHE v1.0.0
                var news = new ModNews
                {
                    Number = 70002,
                    Title = "Town of Host: Enhanced v1.0.0",
                    SubTitle = "★★ 新時代的開始 ★★",
                    ShortTitle = "TOH: Enhanced v1.0.0",
                    BeforeNumber = 70001,
                    Text = "<size=150%>歡迎來到 TOH: Enhanced v1.0.0!</size>\n\n<size=125%>支援版本 Among Us v2023.7.11、v2023.7.12</size>\n" +
                    "\n<b>【基於版本】</b>\n - 基於TOH-RE v3.1.0 (備註: 目前可以使用)\r\n" +
                    "\n<b>【更改/修復】</b>" +
                    "\n     - 刪除了與Loonie的所有關聯，以及README.md中的Credits" +
                    "\n     - (僅限英語翻譯)更改職業名稱 Jailor -> Jailer (you're welcome, ryuk)" +
                    "\n     - 更新了模板/字串/參數" +
                    "\n     - 修復了強盜字串" +
                    "\n\n★ 讓模組變得更好了，歡迎來到 Town of Host: Enhanced! ★",
                    Date = "2023-10-15T00:00:00Z",
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
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
                var news = new ModNews
                {
                    Number = 90002,
                    Title = "Town of Host: Enhanced v1.0.1",
                    SubTitle = "★★ Новая Эра ★★",
                    ShortTitle = "TOH: Enhanced v1.0.1",
                    BeforeNumber = 90001,
                    Text = "<size=150%>Добро Пожаловать в TOH: Enhanced v1.0.1!</size>" +
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
                    Text = "<size=150%>Добро Пожаловать в TOH: Enhanced v1.0.0!</size>" +
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
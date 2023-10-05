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
                // When making new changes/roles, add information
                // TOHE v3.1.0
                var news = new ModNews
                {
                    Number = 100003,
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
            {
                // TOHE v3.0.0
                var news = new ModNews
                {
                    Number = 100002,
                    Title = "Town of Host Re-Edited v3.0.0",
                    SubTitle = "★★The next big update!★★",
                    ShortTitle = "★TOH-RE v3.0.0",
                    Text = "<size=150%>Welcome to TOHE v3.0.0!</size>\n\n<size=125%>Support for Among Us v2023.7.11 and v2023.7.12</size>\n"

                        + "\n<b>【Base】</b>\n - Base on TOH v4.1.2\r\n"

                        + "\n<b>【New Roles】</b>" +
                        "\n\r<b><i>Impostor: (5 roles)</i></b>" +
                            "\n     - Nuker (hidden)" +
                            "\n     - Pitfall" +
                            "\n     - Godfather" +
                            "\n     - Ludopath" +
                            "\n     - Berserker\n\r" +

                        "\n\r<b><i>Crewmate: (13 roles)</i></b>" +
                            "\n     - Admirer" +
                            "\n     - Copycat" +
                            "\n     - Time Master" +
                            "\n     - Crusader" +
                            "\n     - Reverie" +
                            "\n     - Lookout" +
                            "\n     - Telecommunication" +
                            "\n     - Chameleon" +
                            "\n     - Cleanser" +
                            "\n     - Lighter (Add-on: Lighter renamed to Torch)" +
                            "\n     - Task Manager" +
                            "\n     - Jailor" +
                            "\n     - Swapper (Experimental role)\n\r" +

                        "\n\r<b><i>Neutral: (17 roles)</i></b>" +
                            "\n     - Amnesiac" +
                            "\n     - Plaguebearer/Pestilence" +
                            "\n     - Masochist" +
                            "\n     - Doomsayer" +
                            "\n     - Pirate" +
                            "\n     - Shroud" +
                            "\n     - Werewolf" +
                            "\n     - Shaman" +
                            "\n     - Occultist" +
                            "\n     - Shade" +
                            "\n     - Romantic (Vengeful Romantic & Ruthless Romantic)" +
                            "\n     - Seeker" +
                            "\n     - Agitater" +
                            "\n     - Soul Collector\n\r" +

                        "\n\r<b><i>Added new faction: Coven: (10 roles)</i></b>" +
                            "\n     - Banshee" +
                            "\n     - Coven Leader" +
                            "\n     - Necromancer" +
                            "\n     - Potion Master" +
                            "\n     - Moved Jinx to coven" +
                            "\n     - Moved Hex Master to coven" +
                            "\n     - Moved Medusa to coven" +
                            "\n     - Moved Poisoner to coven" +
                            "\n     - Moved Ritualist to coven" +
                            "\n     - Moved Wraith to coven\n\r" +

                        "\n\r<b><i>Add-on: (12 add-ons)</i></b>" +
                            "\n     - Ghoul" +
                            "\n     - Unlucky" +
                            "\n     - Oblivious (returned)" +
                            "\n     - Diseased" +
                            "\n     - Antidote" +
                            "\n     - Burst" +
                            "\n     - Clumsy" +
                            "\n     - Sleuth" +
                            "\n     - Aware" +
                            "\n     - Fragile" +
                            "\n     - Repairman" +
                            "\n     - Void Ballot\n\r" +

                        "\n\r<b>【Rework Roles/Add-ons】</b>" +
                            "\n     - Bomber" +
                            "\n     - Medic" +
                            "\n     - Jackal" +
                            "\n     - Trapster" +
                            "\n     - Mare (is now an add-on)\n\r" +

                        "\n\r<b>【Bug Fixes】</b>" +
                            "\n     - Fixed Torch" +
                            "\n     - Fixed fatal error on Crusader" +
                            "\n     - Fixed long loading time for game with mod" +
                            "\n     - Jinx should no longer be able to somehow jinx themself" +
                            "\n     - Cursed Wolf should no longer be able to somehow curse themself" +
                            "\n     - Fixed bug when extra title appears in settings mod" +
                            "\n     - Fixed bug when modded non-host can't guess add-ons and some roles" +
                            "\n     - Fixed bug when some roles could not get Add-ons" +
                            "\n     - Fixed a bug where the text \"Devoured\" did not appear for the player" +
                            "\n     - Fixed bug where non-Lovers players would dead together" +
                            "\n     - Fixed bug when shield in-lobby caused by Vulture cooldown up" +
                            "\n     - Fixed bug where role Tracefinder sometime did not have arrows" +
                            "\n     - Fixed bug when setting \"Neutrals Win Together\" doesn't work" +
                            "\n     - Fixed Bug When Some Neutrals Cant Click Sabotage Button (Host)" +
                            "\n     - Fixed Bug When Puppeteer and Witch Dont Sees Target Mark" +
                            "\n     - Fixed Zoom" +
                            "\n     - Some fixes black screen" +
                            "\n     - Some Fix for Sheriff" +
                            "\n     - Fixed Tracker Arrow" +
                            "\n     - Fixed Divinator" +
                            "\n     - Fixed some add-on conflicts" +
                            "\n     - Fixed Report Button Icon\n\r" +

                        "\n\r<b>【Improvements Roles】</b>" +
                            "\n     - Fortune Teller" +
                            "\n     - Serial Killer" +
                            "\n     - Camouflager" +
                            "\n     - Retributionist Balancing" +
                            "\n     - Setting: Arsonist keeps the game going" +
                            "\n     - Vector setting: Vent Cooldown" +
                            "\n     - Jester setting: Impostor Vision" +
                            "\n     - Avenger setting: Crewmates/Neutrals can become Avenger" +
                            "\n     - Judge Can TrialIs Coven" +
                            "\n     - Setting: Torch is affected by Lights Sabotage" +
                            "\n     - SS Duration and CD setting for Miner and Escapist" +
                            "\n     - Added ability use limit for Time Master and Grenadier" +
                            "\n     - Added the ability to increase abilities when completing tasks for: Coroner, Chameleon, Tracker, Mechanic, Oracle, Inspector, Medium, Fortune Teller, Grenadier, Veteran, Time Master, and Pacifist" +
                            "\n     - Setting to hide the shot limit for Sheriff" +
                            "\n     - Setting for Fortune Teller whether it shows specific roles instead of clues on task completion" +
                            "\n     - Settings for Tracefinder that determine a delay in when the arrows show up" +
                            "\n     - Setting for Mortician whether it has arrows toward bodies or not" +
                            "\n     - Setting for Oracle that determines the chance of showing an incorrect result" +
                            "\n     - Settings for Mechanic that determine how many uses it takes to fix Reactor/O2 and Lights/Comms" +
                            "\n     - Setting for Swooper, Wraith, Chameleon, and Shade that determines if the player can vent normally when invisibility is on cooldown" +
                            "\n     - Setting: Bait Can Be Reported Under All Conditions" +
                            "\n     - Chameleon uses the engineer vent cooldown" +
                            "\n     - Vampire and Poisoner now have their cooldowns reset when the bitten/poisoned player dies\n\r" +

                        "\n\r<b>【New Client Settings】</b>" +
                            "\n     - Show FPS" +
                            "\n     - Game Master (GM) (has been moved)" +
                            "\n     - Text Overlay" +
                            "\n     - Small Screen Mode" +
                            "\n     - Old Role Summary\n\r" +

                        "\n\r<b>【New Mod Settings】</b>" +
                            "\n     - Use Protection Anti Blackout" +
                            "\n     - Killer count command (Also includes /kcount command)" +
                            "\n     - See ejected roles in meetings" +
                            "\n     - Remove pets at dead players (Vanilla bug fix)" +
                            "\n     - Setting to disable unnecessary shield animations" +
                            "\n     - Setting to hide the kill animation when guesses happen" +
                            "\n     - Disable comms camouflage on some maps" +
                            "\n     - Block Switches When They Are Up" +
                            "\n     - Sabotage Cooldown Control" +
                            "\n     - Reset Doors After Meeting (Airship/Polus)\n\r" +

                        "\n\r<b>【Some Changes】</b>" +
                            "\n     - Victory and Defeat text is no longer role colored" +
                            "\n     - Last Impostor can no longer be guessed" +
                            "\n     - Tasks from a crewmate lover now count towards a task win" +
                            "\n     - Infected players now die after a meeting if there's no alive Infectious" +
                            "\n     - Body reports during camouflage is now separated" +
                            "\n     - Trapster, Vector, Egoist, Revolutionist, Provocateur, Guesser are no longer experimental" +
                            "\n     - Added ability to change settings by 5 instead of 1 when holding the Left/Right Shift key" +
                            "\n     - All ability cooldowns are now reset after meetings" +
                            "\n     - Lovers can not become Sunnyboy" +
                            "\n     - Task bar always set to none" +
                            "\n     - Hangman moved to experimental due to bugs" +
                            "\n     - Roles with an add-on equivalent will not spawn if the add-on is enabled" +
                            "\n     - \"/r\" command has been improved\n\r" +

                        "\n\r<b>【New Features】</b>" +
                            "\n     - Load time reduced significantly" +
                            "\n     - The mod has been translated to Spanish (Partially)" +
                            "\n     - The mod has been translated to Chinese" +
                            "\n     - Improvement Random Map" +
                            "\n     - Main menu has been changed" +
                            "\n     - Added new buttons in main menu" +
                            "\n     - Added Auto starting features" +
                            "\n     - Reworked Discord Rich Presence" +
                            "\n     - Moderator and Sponsor improvement (/kick, /ban, /warn, and Moderator tags)" +
                            "\n     - Default template file has been updated" +
                            "\n     - Reworked end game summary (In the settings you can also return the old)" +
                            "\n     - Improvement platform kick" +
                            "\n     - Check Supported Version Among Us\n\r" +

                        "\n\r<b>【Removals】</b>" +
                            "\n     - Removed Solo PVP mode" +
                            "\n     - Removed Neptune" +
                            "\n     - Removed Capitalist",

                    Date = "2023-9-16T00:00:00Z"

                };
                AllModNews.Add(news);
            }

            {
                // TOHE v2.5.0
                var news = new ModNews
                {
                    Number = 100001,
                    Title = "TownOfHostEdited v2.5.0",
                    SubTitle = "★★★★Another big update, maybe bigger?★★★★",
                    ShortTitle = "★TOHE v2.5.0",
                    Text = "<size=150%>Welcome to TOHE v2.5.0.</size>\n\n<size=125%>Support for Among Us v2023.7.11 and v2023.7.12</size>\n"

                        + "\n【Base】\n - Base on TOH v4.1.2\r\n"
                        + "\n【Fixes】\n - Various bug fixes\n\r"
                        + "\n【Changes】\n - Hex Master hex icon changed to separate it from Spellcaster\n - Fortune Teller moved to Experimentals due to a planned and unfinished rework\n\r"

                        + "\n【New Features】\n - New role: Twister (role by papercut on Discord)\n\r - New role: Chameleon (from Project: Lotus)\n\r - New role: Morphling\n\r - New role: Inspector (role by ryuk on Discord)\n\r - New role: Medusa\n\r - New add-on: Lazy\n\r - New add-on: Gravestone\n\r - New add-on: Autopsy (from TOHY)\n\r - New add-on: Loyal\n\r - New add-on: Visionary\n\r- New experimental role: Spiritcaller (role by papercut on Discord)\n\r"

                        + "\n【Role Changes】\n - Various changes were made, such as an update to Opportunist\n\r",

                    Date = "2023-7-14T00:00:00Z"

                };
                AllModNews.Add(news);
            }

            {
                // TOHE v2.4.2
                var news = new ModNews
                {
                    Number = 100000,
                    Title = "TownOfHostEdited v2.4.2",
                    SubTitle = "★★★★Ooooh bigger update★★★★",
                    ShortTitle = "★TOHE v2.4.2",
                    Text = "Added in some new stuff, along with some bug fixes.\r\nAmong Us v2023.3.28 is recommended so the roles work correctly.\n"

                        + "\n【Base】\n - Base on TOH v4.1.2\r\n"
                        + "\n【Fixes】\n - Fixed various black screen bugs (some still exist but should be less common)\r\n - Other various bug fixes (they're hard to keep track of)\r\n"
                        + "\n【Changes】\n - Judge now supports Guesser Mode\r\n - Background image reverted to use the AU v2023.3.28 size due to the recommended Among Us version being v2023.3.28\r\n - Many other unlisted changes\r\n - Mario renamed to Vector due to copyright concerns\r\n"

                        + "\n【New Features】\n - ###Impostors\n - Councillor\r\n - Deathpact (role by papercut on Discord)\r\n - Saboteur (25% chance to replace Inhibitor)\r\n - Consigliere (by Yumeno from TOHY)\r\n - Dazzler (role by papercut on Discord)\r\n - Devourer (role by papercut on Discord)\r\n"
                        + "\n ### Crewmates\n - Addict (role by papercut on Discord)\r\n - Tracefinder\r\n - Deputy\r\n - Merchant (role by papercut on Discord)\r\n - Oracle\r\n - Spiritualist (role by papercut on Discord)\r\n - Retributionist\r\n- Guardian\r\n - Monarch\r\n"
                        + "\n ### Neutrals\n - Maverick\r\n - Cursed Soul\r\n - Vulture (role by ryuk on Discord)\r\n - Jinx\r\n - Pickpocket\r\n - PotionMaster\r\n - Traitor\r\n"
                        + "\n ### Add-ons\n - Double Shot (add-on by TommyXL)\r\n - Rascal\r\n"

                        + "\n【Role Changes】\n - Mimic now has a setting to see the roles of dead players, due to how useless this add-on was\r\n - A revealed Workaholic can no longer be guessed\r\n - Doctor has a new setting like Workaholic to be revealed to all (currently exposes evil Doctors, use at your own risk)\r\n - Mayor has a setting for a TOS mechanic to reveal themselves\r\n - Warlock balancing\r\n - Cleaner balancing (resets kill cooldown to value set in Cleaner settings)\r\n - Updated Monarch\r\n- Removed speed boost from Mare\r\n"
                        + "\n【Removals】\n - Removed Flash\r\n - Removed Speed Booster\r\n - Temporarily removed Oblivious",

                    Date = "2023-7-5T00:00:00Z"

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
                // TOHE v3.1.0
                var news = new ModNews
                {
                    Number = 80003,
                    Title = "Town of Host Re-Edited v3.1.0",
                    SubTitle = "★★更新较小，但仍然有点大★★",
                    ShortTitle = "★TOH-RE v3.1.0",
                    Text = "<size=150%>欢迎来到 TOHE v3.1.0!</size>\n\n<size=125%>支持 Among Us v2023.7.11 和 v2023.7.12</size>\n"

                        + "\n<b>【对应官方版本】</b>\n - 基于官方版本 v4.1.2\r\n"

                        + "\n<b>【身份新增】</b>" +
                        "\n\r<b><i>内鬼: (3 个新身份)</i></b>" +
                            "\n     - 坏迷你船员" +
                            "\n     - 暗杀者\n\r" +
                            "\n     - 吸血鬼女王(来自TOHTOR)\n\r" +

                        "\n\r<b><i>船员: (1 个新身份)</i></b>" +
                            "\n     - 好迷你船员\n\r" +

                        "\n\r<b><i>中立: (2 个新身份)</i></b>" +
                            "\n     - 强盗" +
                            "\n     - 模仿者\n\r" +

                            "\n\r<b><i>附加职业 (1 个附加职业)</i></b>" +
                            "\n     - 网络人\n\r" +

                        "\n\r<b>【Bug修复】</b>" +
                            "\n     - 修复了背叛的愚者" +
                            "\n     - 修复了狼人（不再留下重复的尸体）" +
                            "\n     - 修复了换票师会踢出客户端(Mod)端" +
                            "\n     - 修复了自我主义者获胜画面显示双重的bug（希望如此）" +
                            "\n     - 修复了抹除者的小问题" +
                            "\n     - 修复了律师和处刑者有任务时的bug" +
                            "\n     - 修复了巫师和魅影的获胜条件" + 
                            "\n     - 修复了死神和幻想家死亡时还能使用技能的bug" +
                            "\n     - 修复了在启用\"可以猜测附加职业\"设置的情况下，导致某些身份在某些条件下无法猜测附加职业的bug\n\r" +

                        "\n\r<b>【身份更改】</b>" +
                            "\n     - 正义追踪者" +
                            "\n     - 失忆者\n\r" +

                        "\n\r<b>【新客户端(Mod)设置】</b>" +
                            "\n     - 设置：通风口随机出生\n\r" +

                        "\n\r<b>【一些改变】</b>" +
                            "\n     - \"设置默认的所有TOHE选项\"已返厂" +
                            "\n     - 玩家现在是带刀中立（以前是邪恶中立）" +
                            "\n     - 自爆兵、核武器和医生不再获得脆弱" +
                            "\n     - 一些中立身份被赋予了\"可以使用通风口\"和\"拥有内鬼视野\"的设置" +
                            "\n     - 法官现在被视为带刀船员" +
                            "\n     - 船员和内鬼标签已重新整理" +
                            "\n     - 当某些身份的技能结束时，如果启用\"隐藏投票\"的设置，他们的投票将不再隐藏" +
                            "\n     - 添加了防止黑屏但带刀中立数量大于 1 时的警告\n\r" +

                        "\n\r<b>【迁移】</b>" +
                            "\n     - 删除了巫师阵营(投毒者、扫把星、药剂师、美杜莎、魅影和巫师又重新成为带刀中立\n遮蔽者、亡灵巫师、巫师领袖和祭祀者现在未被使用)\n\r" +

                        "\n\r<b>【删除】</b>" +
                            "\n     - 删除了巫师阵营",

                    Date = "2023-9-24T00:00:00Z"

                };
                AllModNews.Add(news);
            }

            {
                // TOHE v3.0.0
                var news = new ModNews
                {
                    Number = 80002,
                    Title = "Town of Host Re-Edited v3.0.0",
                    SubTitle = "★★下一次的重大更新！★★",
                    ShortTitle = "★TOH-RE v3.0.0",
                    Text = "<size=150%>欢迎来到 TOHE v3.0.0!</size>\n\n<size=125%>支持 Among Us v2023.7.11 和 v2023.7.12</size>\n"

                        + "\n<b>【对应官方版本】</b>\n - 基于官方版本 v4.1.2\r\n"

                        + "\n<b>【身份新增】</b>" +
                        "\n\r<b><i>内鬼: (5 个新身份)</i></b>" +
                            "\n     - 核武器 (隐藏)" +
                            "\n     - 设陷者" +
                            "\n     - 教父" +
                            "\n     - 速度者" +
                            "\n     - 狂战士\n\r" +

                        "\n\r<b><i>船员: (13 个新身份)</i></b>" +
                            "\n     - 仰慕者" +
                            "\n     - 模仿猫" +
                            "\n     - 时间之主" +
                            "\n     - 十字军" +
                            "\n     - 遐想者" +
                            "\n     - 瞭望员" +
                            "\n     - 通信员" +
                            "\n     - 变色龙" +
                            "\n     - 清洗者" +
                            "\n     - 执灯人 (附加职业: 执灯人改名为火炬)" +
                            "\n     - 任务管理者" +
                            "\n     - 狱警" +
                            "\n     - 换票师 (实验性身份)" +

                        "\n\r<b><i>中立: (17 个新身份)</i></b>" +
                            "\n     - 失忆者" +
                            "\n     - 瘟疫使者/瘟疫" +
                            "\n     - 受虐狂" +
                            "\n     - 末日赌怪" +
                            "\n     - 决斗者" +
                            "\n     - 裹尸布" +
                            "\n     - 月下狼人" +
                            "\n     - 萨满" +
                            "\n     - 神秘者" +
                            "\n     - 遮蔽着" +
                            "\n     - 搜寻者" +
                            "\n     - 煽动者" +
                            "\n     - 浪漫者 (复仇浪漫者 & 无情浪漫者)" +
                            "\n     - 灵魂收集者\n\r" +

                        "\n\r<b><i>新增阵营: 巫师: (10 个新身份)</i></b>" +
                            "\n     - 护盾巫师" +
                            "\n     - 巫师首领" +
                            "\n     - 亡灵巫师" +
                            "\n     - 药剂师" +
                            "\n     - 将扫把星移至巫师阵营" +
                            "\n     - 将巫师移至巫师阵营" +
                            "\n     - 将美杜莎移至巫师阵营" +
                            "\n     - 将投毒者移至巫师阵营" +
                            "\n     - 将祭祀者移至巫师阵营" +
                            "\n     - 将魅影移至巫师阵营\n\r" +

                        "\n\r<b><i>附加职业: (12 附加职业)</i></b>" +
                            "\n     - 食尸鬼" +
                            "\n     - 倒霉蛋" +
                            "\n     - 不受重视 (返厂)" +
                            "\n     - 患病" +
                            "\n     - 健康" +
                            "\n     - 爆破者" +
                            "\n     - 笨蛋" +
                            "\n     - 侦察员" +
                            "\n     - 意识到" +
                            "\n     - 脆弱" +
                            "\n     - 维修员" +
                            "\n     - 无效投票\n\r" +

                        "\n\r<b>【身份/附加职业 重做】</b>" +
                            "\n     - 自爆兵" +
                            "\n     - 医生" +
                            "\n     - 豺狼" +
                            "\n     - 诡雷" +
                            "\n     - 梦魇 (现在是附加职业)\n\r" +

                        "\n\r<b>【Bug修复】</b>" +
                            "\n     - 修复了火炬" +
                            "\n     - 修复了十字军致命Bug" +
                            "\n     - 修复了装有 MOD 的玩家游戏加载时间过长的问题" +
                            "\n     - 扫把星不会再莫名其妙地给自己带来厄运" +
                            "\n     - 呪狼不会再以某种方式诅咒自己" +
                            "\n     - 修复了设置模式中出现额外标题时的Bug" +
                            "\n     - 修复了修改后的非房主无法猜测附加职业和某些身份的Bug" +
                            "\n     - 修复了某些身份无法获得附加职业的Bug" +
                            "\n     - 修复了玩家的 \" 小 黑 人 \"文本不会出现的Bug" +
                            "\n     - 修复了非恋人玩家会死在一起的Bug" +
                            "\n     - 修复了秃鹫冷却时间增加导致护盾失效时的Bug" +
                            "\n     - 修复了寻迹者有时没有箭头的Bug" +
                            "\n     - 修复了设置 \"中立玩家共赢\" 不起作用" +
                            "\n     - 修复了一些中立玩家无法点击破坏按钮的Bug (房主)" +
                            "\n     - 修复了傀儡师和女巫看不到目标标记时的Bug" +
                            "\n     - 修复了僵尸" +
                            "\n     - 一些黑屏修复" +
                            "\n     - 警长的一些修复" +
                            "\n     - 修复了追踪者的箭头" +
                            "\n     - 修复了调查员" +
                            "\n     - 修复了一些附加职业的冲突" +
                            "\n     - 修复了报告按钮图标\n\r" +

                        "\n\r<b>【身份更改】</b>" +
                            "\n     - 占卜师" +
                            "\n     - 连环杀手" +
                            "\n     - 隐蔽者" +
                            "\n     - 惩罚者得到了平衡" +
                            "\n     - 设置: 纵火犯让游戏继续进行" +
                            "\n     - 马里奥设置: 使用通风管冷却时间" +
                            "\n     - 豺狼设置: 内鬼视野" +
                            "\n     - 复仇者设置: 船员阵营/中立阵营 可以成为复仇者" +
                            "\n     - 法官现在可以审判巫师阵营" +
                            "\n     - 设置: 火炬会受到灯光破坏的影响" +
                            "\n     - 矿工和逃逸者的持续时间和CD设置" +
                            "\n     - 添加了时间之主和掷弹兵技能使用限制" +
                            "\n     - 添加了在完成任务时增加技能次数的身份： 验尸官、变色龙、正义追踪者、修理工、神谕、检查员、通灵师、占卜师、掷弹兵、老兵、时间之主和和平之鸽" +
                            "\n     - 隐藏警长执法次数显示的设置" +
                            "\n     - 添加了调查员是否显示具体身份的设置" +
                            "\n     - 添加了寻迹者的箭头显示延迟时间的设置" +
                            "\n     - 添加了入殓师是否有箭头指向尸体的设置" +
                            "\n     - 添加了神谕查验的显示错误结果概率的设置" +
                            "\n     - 添加了修理工修复 反应堆/氧气 和 灯光/通信 所需的技能次数的设置" +
                            "\n     - 添加了隐匿者、魅影、变色龙 和 遮蔽者 的玩家在隐形冷却时是否可以正常使用通风管的设置" +
                            "\n     - 设置：在任何情况下都可以强制报告诱饵尸体" +
                            "\n     - 变色龙使用工程师通风口冷却时间" +
                            "\n     - 吸血鬼和投毒者现在会在 被咬/中毒 的玩家死亡时重置冷却时间\n\r" +

                        "\n\r<b>【新客户端(Mod)选项】</b>" +
                            "\n     - 显示 FPS (帧数)" +
                            "\n     - 管理员(GM) (已被转移)" +
                            "\n     - 文本覆盖" +
                            "\n     - 小屏幕模式" +
                            "\n     - 显示游戏结果\n\r" +

                        "\n\r<b>【新Mod设置】</b>" +
                            "\n     - 使用保护功能，防止黑屏" +
                            "\n     - 带刀玩家统计指令（还包括 /kcount 指令）" +
                            "\n     - 查看驱逐时的会议身份" +
                            "\n     - 移除死亡玩家的宠物的设置(修复原版Bug)" +
                            "\n     - 禁用不必要的护盾动画的设置" +
                            "\n     - 禁用猜测时隐藏击杀动画的设置" +
                            "\n     - 在特定地图上禁用破坏通信伪装的设置" +
                            "\n     - 当电力恢复后阻止关闭的设置" +
                            "\n     - 修理冷却时限" +
                            "\n     - 会议后重置门的开关(飞艇地图/波鲁斯）\n\r" +

                        "\n\r<b>【一些改变】</b>" +
                            "\n     - 胜利和失败文本不再使用身份颜色" +
                            "\n     - 仅存内鬼无法被赌" +
                            "\n     - 船员恋人的任务现在可计入任务胜利" +
                            "\n     - 如果没有活着的感染者，被感染的玩家现在会在会议后死亡" +
                            "\n     - 隐蔽过程中的尸体报告现已分离" +
                            "\n     - 诡雷、马里奥、利己主义者、革命家、自爆卡车、赌怪 不再是实验性身份" +
                            "\n     - 添加了按住 左/右 Shift 键时以 5 而不是 1 更改设置的功能" +
                            "\n     - 所有职业的技能冷却时间都会在会议后重置" +
                            "\n     - 恋人不能成为阳光开朗大男孩" +
                            "\n     - 任务进度条更新进度始终设置为 无" +
                            "\n     - 由于存在错误，刽子手已移至实验性身份" +
                            "\n     - 如果启用了附加职业，具有等效附加组件的身份将不会生成" +
                            "\n     - \"/r\" 指令进行了改进\n\r" +

                        "\n\r<b>【新功能】</b>" +
                            "\n     - 加载时间大幅缩短" +
                            "\n     - 该 Mod 已被翻译成西班牙语（部分）" +
                            "\n     - 该 Mod 已被翻译成简体中文" +
                            "\n     - 改进随机地图" +
                            "\n     - 主菜单已更改" +
                            "\n     - 在主菜单中添加了新按钮" +
                            "\n     - 添加了自动启动功能" +
                            "\n     - 在Discord状态栏显示房间代码" +
                            "\n     - 改进协管和赞助商（/kick、/ban、/warn 和协管标签）" +
                            "\n     - 已更新默认template文件" +
                            "\n     - 重做了游戏结束时的总结（在设置中也可以返回旧版总结）" +
                            "\n     - 改进除pc外的设备踢出" +
                            "\n     - 检查 Among Us 支持的Mod版本\n\r" +

                            "\n\r<b>【删除】</b>" +
                            "\n     - 删除了个人竞技模式" +
                            "\n     - 删除海王" +
                            "\n     - 删除资本家",
                            
                    Date = "2023-9-16T00:00:00Z"

                };
                AllModNews.Add(news);
            }

            {
                // TOHE v2.5.0
                var news = new ModNews
                {
                    Number = 80001,
				    Title = "TownOfHostEdited v2.5.0",
				    SubTitle = "★★★★又是一次大更新，也许更大？★★★★",
				    ShortTitle = "★TOHE v2.5.0",
                    Text = "<size=150%>欢迎来到 TOHE v2.5.0.</size>\n\n<size=125%>支持 Among Us v2023.7.11 和 v2023.7.12</size>\n"

                        + "\n【对应官方版本】\n - 基于官方版本 v4.1.2\r\n"
                        + "\n【修正】\n - 各种错误的修复\n\r"
                        + "\n【更改】\n - 妖术图标更改为与巫师分开\n - 由于计划和未完成的工作，占卜师搬到了实验性身份里\n\r"

                        + "\n【身份新增】\n - 新内鬼身份: 龙卷风 \n\r - 新船员身份: 变色龙 \n\r - 新内鬼身份: 化形者\n\r - 新船员身份: 检查员 \n\r - 新中立身份: 美杜莎\n\r - 新附加职业: 懒人\n\r - 新附加职业: 墓碑\n\r - 新附加职业: 尸检 (来自 TOHY)\n\r - 新附加职业: 忠诚 \n\r - 新附加职业: 窥探者 \n\r- 新的实验性身份: 灵魂召唤者 \n\r"

                        + "\n【身份更改】\n - 进行了各种更改，例如更新了投机者\n\r",

				    Date = "2023-7-14T00:00:00Z",

                };
                AllModNews.Add(news);
            }

            {
                // TOHE v2.4.2
                var news = new ModNews
                {
                    Number = 80000,
				    Title = "TownOfHostEdited v2.4.2",
				    SubTitle = "★★★★哦，更大的更新★★★★",
				    ShortTitle = "★TOHE v2.4.2",
                    Text = "添加了一些新内容，以及一些错误修复.\r\nAmong Us v2023.3.28 是推荐的，以便身份正常游玩\n"

                        + "\n【对应官方版本】\n - 基于官方版本 v4.1.2\r\n"
                        + "\n【修正】\n - 修复了各种黑屏错误 (有些仍然存在，但应该不那么常见)\r\n - 其他各种错误修复 (他们很难追踪)\n\r"
                        + "\n【更改】\n - 法官现在支持猜测模式\r\n - 背景图像恢复为使用 AU v2023.3.28 的大小，由于推荐 Among Us 版本为 v2023.3.28\r\n - 许多其他未列出的变化\r\n - 出于版权考虑，马里奥更名为Vector\r\n"

                        + "\n【身份新增】\n - ###内鬼 \n - 议员 \r\n - 死亡契约 \r\n - 破坏者 (更换抑郁者的概率为25%) \r\n - 军师 \r\n - 眩晕者 \r\n - 吞噬者 \r\n\n ### 船员 \n - 瘾君子 \r\n - 寻迹者 \r\n - 捕快 \r\n - 商人 \r\n - 神谕 \r\n - 灵魂论者 \r\n - 惩罚者 \r\n- 守护者 \r\n - 君主 \r\n\n ### 中立 \n - 独行者 \r\n - 被诅咒的灵魂 \r\n - 秃鹫 \r\n - 扫把星 \r\n - 小偷 \r\n - 祭祀者 \r\n - 背叛者 \r\n\n ### 附加职业 \n - 双重猜测 \r\n - 流氓 \r\n"

                        + "\n【身份更改】\n - 宝箱怪现在有了一个可以看到死去玩家的身份设置，因为这个附加职业是多么的无用 \r\n - 一个暴露的工作狂再也不怕被赌死了 \r\n - 医生有一个像工作狂这样的设置将向所有人展示(目前暴露邪恶的医生，使用风险自负) \r\n - 市长有一个TOS机械师展示自己的场景 \r\n - 巫师平衡 \r\n - 清理工平衡 (将击杀冷却时间重置为清洁工设置中设置的值) \r\n - 更新君主 \r\n- 删除了增速者的速度提升 \r\n"
                        + "\n【删除】\n - 删除了闪电侠 \r\n - 删除了增速者 \r\n - 暂时被移走了，被遗忘了 ",

				    Date = "2023-7-5T00:00:00Z"

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
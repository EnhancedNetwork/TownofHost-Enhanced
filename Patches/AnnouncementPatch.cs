using AmongUs.Data;
using AmongUs.Data.Player;
using Assets.InnerNet;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TOHE;

// ##https://github.com/0xDrMoe/TownofHost-Enhanced
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
                    Number = 100007,
                    Title = "Town of Host: Enhanced v1.4.0",
                    SubTitle = "★★ Consistent Updates, yay! ★★",
                    ShortTitle = "TOH: Enhanced v1.4.0",
                    BeforeNumber = 100006,
                    Text = "<size=150%>Welcome to TOH: Enhanced v1.4.0!</size>\n" +
                    "\n<b>【Base】</b>\n - Base on TOH: Enhanced v1.3.0\r\n" +

                    "\n<b>【New Roles/Addons】(7 roles, 3 Addons)</i></b>" +
                        "\n     - Benefactor (Support Crewmate - By: ryuk)" +
                        "\n     - Keeper (Support Crewmate - By: ryuk)" +
                        "\n     - Captain (Power Crewmate - By: ryuk)" +
                        "\n     - Mole (Basic Crewmate - By: ryuk)" +
                        "\n     - Nice Guesser (Basic Crewmate - By: ryuk)" +
                        "\n     - Kamikaze (Support Impostor - By: Drakos)" +
                        "\n     - Solsticer (Experimental Neutral - By: NikoCat223)" +
                        "\n     - Flash (Helpful Addon - By: NikoCat223)" +
                        "\n     - Silent (Helpful Addon - By: NikoCat223)" +
                        "\n     - Mundane (Harmful Addon - By: ryuk)" +

                    "\n<b>【New Settings】</b>" +
                        "\n     - dlekS ehT !paM weN (Thanks sleepyut (@Galster-dev on GitHub) and TommyXL)" +
                        "\n     - New Gamemode: FFA from TOHE+ (By: ryuk, Special Thanks: Gurge44)" +
                        "\n     - Added chat commands /tpin, /tpout - TP players in and out of ship in lobby (By: ryuk)" +
                        "\n     - New Setting: Prevent /quit due to malicious use (By: Furo)" +
                        "\n     - New Setting: Change Decontamination Time (Very Cool! Try this! By: TommyXL)" +
                        "\n     - Returned Setting: Remove Pets At Dead Players " +
                        "\n     - New Region: Modded South America - MSA (By: Pietro)" +
                        "\n     - New Region: Modded Chinese - Multiple (By: NikoCat223)" +
                        "\n     - New Button: Update! Now update the mod automatically! (By: Pietro)" +

                    "\n<b>【Changes】</b>" +
                        "\n     - Added Skill Icons: Vulture, Pursuer and Cleaner (By: LeziYa)" +
                        "\n     - Updated Log Readability (By: TommyXL)" +
                        "\n     - Enhanced Anti-Cheat (EAC) now done by API (By: ryuk & Moe)" +
                        "\n     - Fool (Addon) now incompatible with Repairman (Addon) to avoid issues (By: ryuk)" +
                        "\n     - System now sends message after clearing - Very Useful! (By: ryuk)" +

                    "\n<b>【Bug Fixes】</b>" +
                        "\n     - Alchemist invalid string fix (By: Drakos)" +
                        "\n     - Votes now return if a player dies mid-round or disconnects (By: NikoCat223, ryuk)" +
                        "\n     - Multiple Bug Fixes (By: NikoCat233, LezaiYa)" +
                        "\n     - Enigma Typo (By: Plaguer)" +
                        "\n     - Prevent-MM-Mass-Shapeshift - Set Cheating Player Notification to \"Notify\" (By: NikoCat223)" +
                        "\n     - Removed Unnecessary roles ferom Guesser GUIs (By: NikoCat223)" +
                        "\n     - Fix Workaholic getting Onbound, Rebound and Double Shot when Anti-Guess Enabled" +

                    "\n<b>【Translator Credits】</b>" +
                        "\n     - Brazilian (By: Dx7405)" +
                        "\n     - Dutch (By: apemv, madmazel_)" +
                        "\n     - French (By: FuroYT, KevOut, Klaomi, Sansationnelle)" +
                        "\n     - Italian (By: alot, Baphojack, Mattix606)" +
                        "\n     - Japanese (By: Sunnyboi)" +
                        "\n     - Latin American (By: CreepPower)" +
                        "\n     - Russian (By: chill_ultimated, Nevermore59, Shoulder Devil, TommyXL)" +
                        "\n     - Simplified Chinese (By: CrewCyan, Hinharrrrr, LezaiYa, NikoCat223)" +
                        "\n     - Spanish (By: Dawson, Sunnyboi, thewhiskas27, xxSShadow)" +
                        "\n     - Traditional Chinese (By: FlyFlyTurtle, Hinharrrrr, netherdragontw, Pomelo_)" +
                        "\n<b> Check out all of our translators on our website</b>\r\n" +

                    "\n<b>【Several Other Misc Fixes by Devs Above!】</b>\r\n" +


                    "\n\n★ Welcome to Town of Host: Enhanced v1.4.0 ★",

                    Date = "2024-1-6T03:00:00Z"
                };
                AllModNews.Add(news);
            }
            {
                var news = new ModNews
                {
                    Number = 100006,
                    Title = "Town of Host: Enhanced v1.3.0",
                    SubTitle = "★★ New Roles? Addons? Bug Fixes?! ★★",
                    ShortTitle = "TOH: Enhanced v1.3.0",
                    BeforeNumber = 100005,
                    Text = "<size=150%>Welcome to TOH: Enhanced v1.3.0!</size>\n" +
                    "\n<b>【Base】</b>\n - Base on TOH: Enhanced v1.2.0\r\n" +

                    "\n<b>【New Roles/Addons】(5 roles, 3 Addons)</i></b>" +
                        "\n     - Instigator (Killing Impostor - By: papercut)" +
                        "\n     - Enigma (Support Crewmate - By: papercut)" +
                        "\n     - Pixie (Benign Neutral - By: ryuk, Idea: Azanthiel)" +
                        "\n     - Taskinator (Benign Neutral - By: ryuk, Idea: Dx)" +
                        "\n     - Randomizer (Basic Crewmate - Idea/Coded By: Night, Improved By: LezaiYa)" +
                        "\n     - Influenced (Harmful Addon - By: NikoCat223)" +
                        "\n     - Hurried (Harmful Addon - By: NikoCat223)" +
                        "\n     - Oiiai (Experimental Addon - By: NikoCat223)\r\n" +

                    "\n<b>【New Settings】</b>" +
                        "\n     - Added anti-spam feature on Lava Chat (By: Broke his pc's stupid, Fixed by: ryuk)" +
                        "\n     - Converted Reverie can kill anyone without repercussions (By: ryuk)" +
                        "\n     - Count grow up time in meeting (for Mini) (By: LezaiYa, NikoCat233)" +
                        "\n     - Egoist Count as Converted Neutral (By: ryuk)" +
                        "\n     - New Camouflage Skins added (By: TommyXL)\r\n" +

                    "\n<b>【Changes】</b>" +
                        "\n     - Cyber and Doppelganger incompatibility (By: ryuk)" +
                        "\n     - Improved Ban System (By: NikoCat223)" +
                        "\n     - Improved Codebase Significantly (By: TommyXL)" +
                        "\n     - Improved Sync Settings (By: TommyXL)" +
                        "\n     - Mare and Stalker anti-spawn on The Fungle (By: TommyXL)" +
                        "\n     - Removed Incompatible Role Assignment (By: NikoCat223)" +
                        "\n     - Role colors now modifiable via RoleColor.dat (By: ryuk)" +
                        "\n     - Updated Dev Tags (By: FuroYT)" +
                        "\n     - Vector and Unlucky no longer compatible (By: ryuk)" +
                        "\n     - Vulture now can not eat cleaned/medusa bodies (By: NikoCat223)\r\n" +

                    "\n<b>【Bug Fixes】</b>" +
                        "\n     - Fixed Death Reason Inconsistency (By: ryuk)" +
                        "\n     - Fixed Double IDs for roles that get them (By: ryuk)" +
                        "\n     - <b>Fixed /death by host showing to all players (By: NikoCat223)</b>" +
                        "\n     - Fixed settings overrides and resets\n<b>Must play one or two games</b> (By: ryuk)" +
                        "\n     - Fixed several roles, addons, teleport bugs (By: NikoCat223, ryuk, TommyXL)\r\n" +

                    "\n<b>【New Languages】</b>" +
                        "\n     - French (By: FuroYT, KevOut, Klaomi, Sansationnelle)" +
                        "\n     - Japanese (By: Sunnyboi)" +
                        "\n     - Latin American (By: CreepPower)" +
                        "\n     - Italian (By: alot, Baphojack, Mattix606)" +
                        "\n     - Spanish (By: xxSShadow)" +
                        "\n     - Traditional Chinese (By: FlyFlyTurtle, Pomelo_)" +
                        "\n<b> Check out all of our translators on our website</b>\r\n" +

                    "\n<b>【Several Other Misc Fixes】</b>\r\n" +

                    "\n<b> Note: Overhead tags, and permissions are now done using Discord Bot.</b>" +
                    "\nJoin the Discord for more information at discord.gg/tohe" +
                    "\nAdditionally, ALL SETTINGS will be reset on update. (sorry, it was necessary)" +


                    "\n\n★ Welcome to Town of Host: Enhanced v1.3.0 ★",

                    Date = "2023-12-2T03:00:00Z"
                };
                AllModNews.Add(news);
            }
            {
                var news = new ModNews
                {
                    Number = 100005,
                    Title = "Town of Host: Enhanced v1.2.0",
                    SubTitle = "★★ The Fungle Support! ★★",
                    ShortTitle = "TOH: Enhanced v1.2.0",
                    BeforeNumber = 100004,
                    Text = "<size=150%>Welcome to TOH: Enhanced v1.2.0!</size>\n" +
                    "\n<b>【Base】</b>\n - Base on TOH: Enhanced v1.1.1\r\n" +

                    "\n<b>【Changes】</b>" +
                        "\n     - Support has been fully added with bug fixes for The Fungle Update" +
                        "\n     - Added Temp Ban feature for players constantly joining/leaving (trolls)\n\r" +

                    "\n\n★ Welcome to Town of Host: Enhanced: Fungle Edition ★",

                    Date = "2023-10-29T00:00:00Z"
                };
                AllModNews.Add(news);
            }
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
                    Number = 90006,
                    Title = "Town of Host: Enhanced v1.3.0",
                    SubTitle = "★★ Новые роли? Атрибуты? Исправление багов?! ★★",
                    ShortTitle = "TOH: Enhanced v1.3.0",
                    BeforeNumber = 90005,
                    Text = "<size=150%>Добро пожаловать в TOH: Enhanced v1.3.0!</size>\n" +
                    "\n<b>【Основан】</b>\n - Основан на TOH: Enhanced v1.2.0\r\n" +

                    "\n<b>【Новые роли/Атрибуты】(5 роли, 3 Атрибута)</i></b>" +
                        "\n     - Зачинщик (Предатель убийца - Автор: papercut)" +
                        "\n     - Энигма (Помогающий Член Экипажа - Автор: papercut)" +
                        "\n     - Пикси (Добрый Нейтрал - Автор: ryuk, Идея: Azanthiel)" +
                        "\n     - Таскинатор (Добрый Нейтрал - Автор: ryuk, Идея: Dx)" +
                        "\n     - Рандомайзер (Базовый Член Экипажа - Идея и код: Night, Улучшен: LezaiYa)" +
                        "\n     - Влиятельный (Вредный Атрибут - Автор: NikoCat223)" +
                        "\n     - Опоздавший (Вредный Атрибут - Автор: NikoCat223)" +
                        "\n     - Туман (Эксперементальный Атрибут - Автор: NikoCat223)\r\n" +

                    "\n<b>【Новые настройки】</b>" +
                        "\n     - Добавлена настройка «Скрывать сообщения игроков при изгании» (Автор: Broke his pc's stupid, Исправлен: ryuk)" +
                        "\n     - Преобразованный Мечтатель может убить кого угодно без каких-либо последствий (Автор: ryuk)" +
                        "\n     - «Может продолжать расти во время встречи» для роли Мини (Автор: LezaiYa, NikoCat233)" +
                        "\n     - «Эгоист считается нейтралом» (Автор: ryuk)" +
                        "\n     - Добавлены новые скины камуфляжа (Автор: TommyXL)\r\n" +

                    "\n<b>【Изменения】</b>" +
                        "\n     - Знаменитый и Двойник теперь несовместимы (Автор: ryuk)" +
                        "\n     - Улучшенна система банов (Автор: NikoCat223)" +
                        "\n     - Улучшенна база кода для улучшения производительности (Автор: TommyXL)" +
                        "\n     - Улучшенна синхронизация настроек (By: TommyXL)" +
                        "\n     - Ночной и Сталкер больше не могут появиться на карте The Fungle (Автор: TommyXL)" +
                        "\n     - Удалено несовместимое назначение ролей (Автор: NikoCat223)" +
                        "\n     - Цвета ролей теперь можно изменить в файле «RoleColor.dat» (Автор: ryuk)" +
                        "\n     - Обновленны теги разработчиков (Автор: FuroYT)" +
                        "\n     - Вектор и Неудачный больше не совместимы (Автор: ryuk)" +
                        "\n     - Стервятник теперь не может есть очищенные/каменные трупы (Автор: NikoCat223)\r\n" +

                    "\n<b>【Исправления багов】</b>" +
                        "\n     - Исправлена синхронизация причины смерти у игроков с модом (Автор: ryuk)" +
                        "\n     - Исправлены двойные идентификаторы (Автор: ryuk)" +
                        "\n     - <b>Исправлен баг когда Хост использовал команду «/death» и она отображалось всем игрокам (Автор: NikoCat223)</b>" +
                        "\n     - Исправлен баг когда после перезахода в игру настройки перемешивались.\n<b>Примечание: Необходимо хотя-бы один раз зайти в настройки мода</b> (Автор: ryuk)" +
                        "\n     - Исправлено несколько ролей, атрибутов, и ошибок телепорта (Автор: NikoCat223, ryuk, TommyXL)\r\n" +

                    "\n<b>【Новые языки】</b>" +
                        "\n     - Французский (Автор: FuroYT, KevOut, Klaomi, Sansationnelle)" +
                        "\n     - Японский (Автор: Sunnyboi)" +
                        "\n     - Латиноамериканская (Автор: CreepPower)" +
                        "\n     - Итальянский (Автор: alot, Baphojack, Mattix606)" +
                        "\n     - Испанский (Автор: xxSShadow)" +
                        "\n     - Традиционный Китайский (Автор: FlyFlyTurtle, Pomelo_)" +
                        "\n<b>Ознакомьтесь со всеми нашими переводчиками на нашем сайте</b>\r\n" +

                    "\n<b>【Несколько других исправлений】</b>\r\n" +

                    "\n<b> Примечание: Накладные теги и разрешения теперь выполняются с помощью бота в Discord</b>" +
                    "\nПрисоединяйтесь к Discord для получения дополнительной информации на: «discord.gg/tohe»" +
                    "\nКроме того, ВСЕ НАСТРОЙКИ будут сброшены при обновлении (извините, так было нужно)" +


                    "\n\n★ Добро пожаловать в Town of Host: Enhanced v1.3.0 ★",

                    Date = "2023-12-2T03:00:00Z"
                };
                AllModNews.Add(news);
            }
            {
                var news = new ModNews
                {
                    Number = 90005,
                    Title = "Town of Host: Enhanced v1.2.0",
                    SubTitle = "★★ Поддержка карты The Fungle! ★★",
                    ShortTitle = "TOH: Enhanced v1.2.0",
                    BeforeNumber = 90004,
                    Text = "<size=150%>Добро пожаловать в TOH: Enhanced v1.2.0!</size>\n" +
                    "\n<b>【Основан】</b>\n - Основан на TOH: Enhanced v1.1.1\r\n" +

                    "\n<b>【Изменения】</b>" +
                        "\n     - Полная поддержка карты The Fungle ​​с исправлениями ошибок" +
                        "\n     - Добавлена ​​функция временного бана для игроков, постоянно присоединяющихся/выходящих (тролли)\n\r" +

                    "\n\n★ Добро пожаловать в Town of Host: Enhanced: Fungle Edition ★",

                    Date = "2023-10-29T00:00:00Z"
                };
                AllModNews.Add(news);
            }
            {
                var news = new ModNews
                {
                    Number = 90004,
                    Title = "Town of Host: Enhanced v1.1.1",
                    SubTitle = "★★ Обновление с починкой багов и оптимизацией! ★★",
                    ShortTitle = "TOH: Enhanced v1.1.1",
                    BeforeNumber = 90003,
                    Text = "<size=150%>Добро пожаловать в TOH: Enhanced v1.1.1!</size>\n" +
                    "\n<b>【Основан】</b>\n - Основан на TOH: Enhanced v1.1.0\r\n" +

                    "\n<b>【Исправления】</b>" +
                        "\n     - Пофикшен баг с (Злым) Мини, где невозможно засудить или отомститьф" +
                        "\n     - Пофикшен Обменщик и некоторые баги с ним, остается эксперементальным" +
                        "\n     - Пофикшено бессмертие Берсерка и спамящщие ошибки" +
                        "\n     - Пофикшен Безумный Добрый Мини" +
                        "\n     - Пофикшено совмещение Пустого и Решающего\n\r" +

                    "\n<b>【Другие исправления】</b>" +
                        "\n     - Улучшили Мясника в PlayerControls" +
                        "\n     - Переработали проверки в Конце Игры (не опять а снова) чтобы их оптимизировать" +

                    "\n\n★ Добро пожаловать в Town of Host: Enhanced! ★",

                    Date = "2023-10-23T00:00:00Z"
                };
                AllModNews.Add(news);
            }
            {
                var news = new ModNews
                {
                    Number = 90003,
                    Title = "Town of Host: Enhanced v1.1.0",
                    SubTitle = "★★ Обновление уже?! Ух ты! ★★",
                    ShortTitle = "TOH: Enhanced v1.1.0",
                    BeforeNumber = 90002,
                    Text = "<size=150%>Добро пожаловать в TOH: Enhanced v1.1.0!</size>\n" +
                    "\n<b>【Основан】</b>\n - Основан на TOH: Enhanced v1.1.0\r\n" +

                    "\n<b>【Новые Роли/Атрибуты】</b>" +
                        "\n     - Главарь (Члены Экипажа: Сильный)" +
                        "\n     - Шпион (Члены Экипажа: Помогающий)" +
                        "\n     - Линчеватель (Члены Экипажа: Убийца)\n\r" +

                        "\n     - Отскок (Атрибут: Смешанный)\n\r" +

                    "\n<b>【New Settings】</b>" +
                        "\n     - Двуликий: «Двуликий телепортируется на убийство» и «Количество выполненных заданий для 1 убийства»" +
                        "\n     - Deathpact: Игроки в активном «заключении договора» могут созвать собрание" +
                        "\n     - Твистер: Скрыть информацию с кем игроки меняются местами\n" +

                        "\n     - Обманщик: Обманщик теряет способность, если обманывает игрока без кнопки убийства" +
                        "\n     - Торговец: Продавать только включенные атрибуты" +
                        "\n     - Коронер: Сообщать убийце что его отслеживают\n\r" +

                        "\n     - Заразный: Двойное нажатие для Убийства/Заражение" +

                        "\n     - Растерянный: Убийца получает дальность обзора Растерянного\n\r" +

                    "\n<b>【Удалённые Роли/Атрибуты】</b>" +
                        "\n     - Нейтрал: Окультист\n\r" +
                        "\n     - Атрибут: Солнцезащитный" +
                        "\n     - Атрибут: Светящийся" +

                    "\n<b>【Другие Изменения】</b>" +
                        "\n     - Новый поддерживаемый язык: Португальский" +
                        "\n     - Новые игры в лобби: /rps & /coinflip" +
                        "\n     - Новый скин у Камуфляжа" +
                        "\n     - Добавлен «Default_Template.txt»" +
                        "\n     - Переработан Мечтатель и Бессердечник" +

                    "\nНесколько других исправлений багов (и когда я говорю несколько, я имею в виду НЕСКОЛЬКО)" +

                    "\n\n★ Добро Пожаловать в Town of Host: Enhanced! ★",

                    Date = "2023-10-21T00:00:00Z"
                };
                AllModNews.Add(news);
            }
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
                // TOHE 1.3.0
                var news = new ModNews
                {
                    Number = 80005,
                    Title = "Town of Host: Enhanced v1.3.0",
                    SubTitle = "★★ 新职业？附加职业？Bug修复？ ★★",
                    ShortTitle = "TOH: Enhanced v1.3.0",
                    BeforeNumber = 80004,
                    Text = "<size=150%>欢迎来到 TOH: Enhanced v1.3.0！</size>\n" +
                    "\n<b>【基于官方版本】</b>\n - 基于 TOH: Enhanced v1.2.0\r\n" +

                    "\n<b>【新职业/附加职业】(5个职业, 3个附加职业)</i></b>" +
                        "\n     - 教唆者 (击杀类内鬼- By: papercut)" +
                        "\n     - 猜想者 (支援类船员 - By: papercut)" +
                        "\n     - 小精灵 (友好类中立 - By: ryuk, 想法: Azanthiel)" +
                        "\n     - 任务执行者 (友好类中立 - By: ryuk, 想法: Dx)" +
                        "\n     - 萧暮 (简单类船员 - 想法和编码 by: Night, 改进 by: LezaiYa)" +
                        "\n     - 影响者 (有害类附加 - By: NikoCat223)" +
                        "\n     - 焦急者 (有害类附加 - By: NikoCat223)" +
                        "\n     - Oiiai (实验性附加 - By: NikoCat223)\r\n" +

                    "\n<b>【新设置】</b>" +
                        "\n     - 添加了防止已死玩家在播放驱逐动画时发消息的功能 (By: 阿龙, 修复 by: ryuk)" +
                        "\n     - 阵营转换后遐想者击杀任何人而不受影响 (By: ryuk)" +
                        "\n     - 计算会议中的成长时间（迷你船员） (By: LezaiYa, NikoCat233)" +
                        "\n     - 利己主义者算作中立 (By: ryuk)" +
                        "\n     - 添加新迷彩皮肤 (By: TommyXL)\r\n" +

                    "\n<b>【其他变化】</b>" +
                        "\n     - 网络员和分身者不兼容 (By: ryuk)" +
                        "\n     - 改进封禁系统 (By: NikoCat223)" +
                        "\n     - 大幅改进源代码库 (By: TommyXL)" +
                        "\n     - 改进了同步设置 (By: TommyXL)" +
                        "\n     - 梦魇和潜藏者不会在真菌密林分配 (By: TommyXL)" +
                        "\n     - 删除了不兼容的职业分配 (By: NikoCat223)" +
                        "\n     - 现在可通过 RoleColor.dat 修改职业颜色 (By: ryuk)" +
                        "\n     - 更新开发人员标签 (By: FuroYT)" +
                        "\n     - 马里奥和倒霉蛋不再兼容 (By: ryuk)" +
                        "\n     - 秃鹫不能吃被清理工清理和美杜莎石化的尸体了 (By: NikoCat223)\r\n" +

                    "\n<b>【Bug修复】</b>" +
                        "\n     - 修复了模组房主和客户端显示死亡原因不一致Bug (By: ryuk)" +
                        "\n     - 修复了显示 ID 职业的双重 ID 的Bug (By: ryuk)" +
                        "\n     - <b>修复了所有玩家都能看到房主/death指令的Bug (By: NikoCat223)</b>" +
                        "\n     - 修复了设置覆盖和重置的Bug\n<b>必须玩一到两场游戏</b> (By: ryuk)" +
                        "\n     - 修复了多个职业、附加职业和传送错误 (By: NikoCat223, ryuk, TommyXL)\r\n" +

                    "\n<b>【新语言】</b>" +
                        "\n     - 法语 (By: FuroYT, KevOut, Klaomi, Sansationnelle)" +
                        "\n     - 日语 (By: Sunnyboi)" +
                        "\n     - 拉丁美洲 (By: CreepPower)" +
                        "\n     - 意大利语 (By: alot, Baphojack, Mattix606)" +
                        "\n     - 西班牙语 (By: xxSShadow)" +
                        "\n     - 繁体中文 (By: FlyFlyTurtle, Pomelo_)" +
                        "\n<b> 在我们的网站上查看我们所有的翻译人员</b>\r\n" +

                    "\n<b>【若干其他杂项修复】</b>\r\n" +

                    "\n<b> 注意：现在使用 Discord 机器人来完成开发标签和权限设置</b>" +
                    "\n加入我们的Discord了解更多信息，请访问：discord.gg/tohe" +
                    "\n此外，更新时将重置所有设置（对不起，这是必要的）" +


                    "\n\n★ 欢迎来到 Town of Host: Enhanced v1.3.0 ★",

                    Date = "2023-12-2T03:00:00Z"
                };
                AllModNews.Add(news);
            }
            {
                // TOHE v1.2.0
                var news = new ModNews
                {
                    Number = 80004,
                    Title = "Town of Host: Enhanced v1.2.0",
                    SubTitle = "★★ 支持真菌密林 ! ★★",
                    ShortTitle = "TOH: Enhanced v1.2.0",
                    BeforeNumber = 80003,
                    Text = "<size=150%>欢迎来到TOH: Enhanced v1.2.0!</size>\n" +
                    "\n<b>【基于官方版本】</b>\n - 基于TOH: Enhanced v1.1.1\r\n" +

                    "\n<b>【其他变化】</b>" +
                        "\n     - 已完全添加支持并修复了 Fungle 更新的错误" +
                        "\n     - 如果有玩家不断加入/离开，则会暂时封禁该玩家\n\r" +

                    "\n\n★ 欢迎来到Town of Host: Enhanced: Fungle Edition ★",

                    Date = "2023-10-29T00:00:00Z"
                };
                AllModNews.Add(news);
            }
            {
                // TOHE v1.1.1
                var news = new ModNews
                {
                    Number = 80003,
                    Title = "Town of Host: Enhanced v1.1.1",
                    SubTitle = "★★ 性能更新和bug修复！★★",
                    ShortTitle = "TOH: Enhanced v1.1.1",
                    BeforeNumber = 80002,
                    Text = "<size=150%>欢迎来到TOH: Enhanced v1.1.1!</size>\n" +
                    "\n<b>【基于官方版本】</b>\n - 基于TOH: Enhanced v1.1.0\r\n" +

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
                // TOHE v1.1.0
                var news = new ModNews
                {
                    Number = 80002,
                    Title = "Town of Host: Enhanced v1.1.0",
                    SubTitle = "★★ 已经更新了？哇哦！ ★★",
                    ShortTitle = "TOH: Enhanced v1.1.0",
                    BeforeNumber = 80001,
                    Text = "<size=150%>欢迎来到TOH: Enhanced v1.1.0！</size>\n" +
                    "\n<b>【基于官方版本】</b>\n - 基于TOH: Enhanced v1.0.1\r\n" +

                    "\n<b>【新职业/附加职业】</b>" +
                        "\n     - 总统（船员阵营：权力）" +
                        "\n     - 间谍（船员阵营：支援）" +
                        "\n     - 义务警长（船员阵营：击杀）\n\r" +

                        "\n     - 回弹者（附加职业：混合）\n\r" +

                    "\n<b>【新设置】</b>" +
                        "\n     - 船鬼:每完成x（用字母x表示设定的任务数）项任务后，立即瞬移并击杀" +
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
                // TOHE v1.0.1
                var news = new ModNews
                {
                    Number = 80001,
                    Title = "Town of Host: Enhanced v1.0.1",
                    SubTitle = "★★ 新时代 ★★",
                    ShortTitle = "TOH: Enhanced v1.0.1",
                    BeforeNumber = 80000,
                    Text = "<size=150%>欢迎来到 TOH: Enhanced v1.0.1！</size>\n\n<size=125%>适配 Among Us v2023.7.11 和 v2023.7.12</size>\n" +
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
                    Number = 80000,
                    Title = "Town of Host: Enhanced v1.0.0",
                    SubTitle = "★★ 新时代 ★★",
                    ShortTitle = "TOH: Enhanced v1.0.0",
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
                var news = new ModNews
                {
                    Number = 70005,
                    Title = "Town of Host: Enhanced v1.3.0",
                    SubTitle = "★★ 新職業? 附加職業? Bug修復?! ★★",
                    ShortTitle = "TOH: Enhanced v1.3.0",
                    BeforeNumber = 70004,
                    Text = "<size=150%>歡迎來到 TOH: Enhanced v1.3.0!</size>\n" +
                    "\n<b>【基於版本】</b>\n - 基於 TOH: Enhanced v1.2.0\r\n" +

                    "\n<b>【新職業/附加職業】(5個主職業，3個附加職業</i></b>" +
                        "\n     - 教唆者 (擊殺類偽裝者 By: papercut)" +
                        "\n     - 猜想者 (支援類船員 - By: papercut)" +
                        "\n     - 精靈 (友善類中立 - By: ryuk, 想法: Azanthiel)" +
                        "\n     - 搗蛋鬼 (友善類中立 - By: ryuk, 想法: Dx)" +
                        "\n     - 隨機者 (基礎船員 - 想法以及代碼by: Night, 改進by: LezaiYa)" +
                        "\n     - 順從者 (有害類附加職業 - By: NikoCat223)" +
                        "\n     - 焦急者 (有害類附加職業 - By: NikoCat223)" +
                        "\n     - Oiiai (試驗性附加職業 - By: NikoCat223)\r\n" +

                    "\n<b>【新設定】</b>" +
                        "\n     - 新增了攔截逐出畫面死人討論的機制(By: 阿龍, 修復並改進by: ryuk)" +
                        "\n     - 陣營轉換後的遐想者可以不受限制的擊殺任何人 (By: ryuk)" +
                        "\n     - 計算迷你船員於會議中的成長時間 (By: LezaiYa, NikoCat233)" +
                        "\n     - 利己主義者視為中立 (By: ryuk)" +
                        "\n     - 新增新迷彩皮膚(By: TommyXL)\r\n" +

                    "\n<b>【更動】</b>" +
                        "\n     - 分身者無法同時成為名人 (By: ryuk)" +
                        "\n     - 改進封禁系統 (By: NikoCat223)" +
                        "\n     - 大幅改進了源代碼庫 (By: TommyXL)" +
                        "\n     - 改進了同步設定 (By: TommyXL)" +
                        "\n     - 獵夢者與潛藏者不會在Fungle地圖中被分配 (By: TommyXL)" +
                        "\n     - 刪除了不兼容的職業分配 (By: NikoCat223)" +
                        "\n     - 職業顏色現在可以於RoleColor.dat中修改 (By: ryuk)" +
                        "\n     - 更新開發者標籤 (By: FuroYT)" +
                        "\n     - 瑪利歐不再能同時成為倒楣蛋 (By: ryuk)" +
                        "\n     - 禿鷲現在無法吃被清理/石化的屍體 (By: NikoCat223)\r\n" +

                    "\n<b>【Bug修復】</b>" +
                        "\n     - 修復了房主與模組客戶端死因不一致的問題 (By: ryuk)" +
                        "\n     - 修復了顯示ID職業的雙重ID的問題 (By: ryuk)" +
                        "\n     - <b>修復了房主使用/death指令會被所有玩家看到的問題 (By: NikoCat223)</b>" +
                        "\n     - 修復了設定被覆蓋或重置的問題\n<b>必須玩一到兩場遊戲</b> (By: ryuk)" +
                        "\n     - 修復了數個職業，附加職業和傳送問題 (By: NikoCat223, ryuk, TommyXL)\r\n" +

                    "\n<b>【支援新語言】</b>" +
                        "\n     - 法語 (By: FuroYT, KevOut, Klaomi, Sansationnelle)" +
                        "\n     - 日語 (By: Sunnyboi)" +
                        "\n     - 拉丁美洲 (By: CreepPower)" +
                        "\n     - 義大利文 (By: alot, Baphojack, Mattix606)" +
                        "\n     - 西班牙文 (By: xxSShadow)" +
                        "\n     - 繁體中文 (By: 柚子, FlyFlyTurtle)" +
                        "\n<b>在我們的官網上查看所有翻譯人員</b>\r\n" +

                    "\n<b>【其他雜項修復】</b>\r\n" +

                    "\n<b> 現在頭頂標籤與權限使用Discord機器人完成</b>" +
                    "\n加入我們的Discord(discord.gg/tohe)以了解更多資訊" +
                    "\n此外，更新時所有設定將會被重置(很抱歉，但這是必要的)" +


                    "\n\n★ 歡迎來到 Town of Host: Enhanced v1.3.0 ★",

                    Date = "2023-12-2T03:00:00Z"
                };
                AllModNews.Add(news);
            }
            {
                var news = new ModNews
                {
                    Number = 70004,
                    Title = "Town of Host: Enhanced v1.2.0",
                    SubTitle = "★★ 支援The Fungle! ★★",
                    ShortTitle = "TOH: Enhanced v1.2.0",
                    BeforeNumber = 70003,
                    Text = "<size=150%>歡迎來到 TOH: Enhanced v1.2.0!</size>\n" +
                        "\n<b>【基於版本】</b>\n - 基於 TOH: Enhanced v1.1.1\r\n" +

                        "\n<b>【更動】</b>" +
                        "\n     - 支援最新的The Fungle更新!" +
                        "\n     - 加入反覆進出會被暫時封禁的機制\n\r" +

                        "\n\n★ 歡迎來到 Town of Host: Enhanced: Fungle版本 ★",

                    Date = "2023-10-29T00:00:00Z"
                };
                AllModNews.Add(news);
            }
            {
                // TOHE v1.1.1
                var news = new ModNews
                {
                    Number = 70003,
                    Title = "Town of Host: Enhanced v1.1.1",
                    SubTitle = "★★性能與修復Bug的更新! ★★",
                    ShortTitle = "TOH: Enhanced v1.1.1",
                    BeforeNumber = 70002,
                    Text = "<size=150%>歡迎來到 TOH: Enhanced v1.1.1!</size>\n" +
                    "\n<b>【基於版本】</b>\n - 基於 TOH: Enhanced v1.1.0\r\n" +

                    "\n<b>【Bug修復】</b>" +
                        "\n     - 修復了(壞)迷你船員無法被審判/復仇" +
                        "\n     - 修復了換票師的數個Bug，目前其仍處於試驗性職業" +
                        "\n     - 修復了狂戰士無法死亡以及刷頻的問題" +
                        "\n     - 修復了背叛的好迷你船員的問題" +
                        "\n     - 修復了一個破平者與虛無的衝突Bug\n\r" +

                    "\n<b>【其他修復】</b>" +
                        "\n     - 改進了PlayerControls中的overkiller功能" +
                        "\n     - 重製了結束遊戲檢查(再次)以最佳化它" +

                    "\n\n★ 歡迎來到 Town of Host: Enhanced! ★",

                    Date = "2023-10-23T00:00:00Z"
                };
                AllModNews.Add(news);
            }
            {
                // TOHE 1.1.0
                var news = new ModNews
                {
                    Number = 70002,
                    Title = "Town of Host: Enhanced v1.1.0",
                    SubTitle = "★★ 已經更新啦? 哇喔!★★",
                    ShortTitle = "TOH: Enhanced v1.1.0",
                    BeforeNumber = 70001,
                    Text = "<size=150%>歡迎來到 TOH: Enhanced v1.1.0!</size>\n" +
                    "\n<b>【基於版本】</b>\n - 基於 TOH: Enhanced v1.0.1\r\n" +

                    "\n<b>【新職業/附加職業】</b>" +
                        "\n     - 總統 (船員陣營:權力)" +
                        "\n     - 間諜 (船員陣營:輔助)" +
                        "\n     - 義警 (船員陣營:殺人)\n\r" +

                        "\n     - 反擊者 (附加職業:混和)\n\r" +

                    "\n<b>【新設定】</b>" +
                        "\n     - 船鬼: 每完成(設定數量)任務後，立刻順移殺人" +
                        "\n     - 簽約人: 被簽約的玩家可以召開會議" +
                        "\n     - 躁動者: 隱藏玩家的交換對象\n" +

                        "\n     - 贗品商: 失去錯誤使用贗品的技能" +
                        "\n     - 商人: 只能銷售已開啟的附加職業" +
                        "\n     - 驗屍官: 告知兇手已被追蹤\n\r" +

                        "\n     - 感染源: 點兩下殺人鍵殺人/感染" +

                        "\n     - 迷幻者: 帶刀玩家可以獲得迷患者的視野\n\r" +

                    "\n<b>【刪除的職業/附加職業】</b>" +
                        "\n     - 中立陣營: 咒魔\n\r" +
                        "\n     - 附加職業: 太陽眼鏡" +
                        "\n     - 附加職業: 發光" +

                    "\n<b>【其他更動】</b>" +
                        "\n     - 增加支援語言: 葡萄牙語" +
                        "\n     - 新的大廳小遊戲: /rps與/coinflip" +
                        "\n     - (僅限英語)將【Agent】重命名為【Evil Tracker】" +
                        "\n     - (僅限英語)將【Disruptor】重命名為【Anti Adminer】" +
                        "\n     - 新的迷彩皮膚" +
                        "\n     - 增加了Default_Template.txt(預設模板.txt)" +
                        "\n     - 重製了遐想者與單身狗" +

                    "\n其他Bug的更動(當我說幾個的時候，我就是指那幾個)" +

                    "\n\n★ 歡迎來到 Town of Host: Enhanced! ★",

                    Date = "2023-10-21T00:00:00Z"
                };
                AllModNews.Add(news);
            }
            {
                var news = new ModNews
                {
                    Number = 70001,
                    Title = "Town of Host: Enhanced v1.0.1",
                    SubTitle = "★★ 新時代 ★★",
                    ShortTitle = "TOH: Enhanced v1.0.1",
                    BeforeNumber = 70000,
                    Text = "<size=150%>歡迎來到 TOH: Enhanced v1.0.1!</size>\n\n<size=125%>支援版本 Among Us v2023.7.11、v2023.7.12</size>\n" +
                "\n<b>【基於版本】</b>\n - 基於TOH-RE v3.1.0 (備註: 目前可以使用)\r\n" +
                "\n<b>【修復】</b>" +
                "\n     - 刪除了大廳中的Loonie標誌，並將其更換" +
                "\n     - 在占卜師中增加了一條更新的線索" +
                "\n     - 修復了模板和VIP清單沒有產生的問題" +
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
                    Number = 70000,
                    Title = "Town of Host: Enhanced v1.0.0",
                    SubTitle = "★★ 新時代的開始 ★★",
                    ShortTitle = "TOH: Enhanced v1.0.0",
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
        // ====== French ======
        else if (TranslationController.Instance.currentLanguage.languageID == SupportedLangs.French)
        {
            {
                var news = new ModNews
                {
                    Number = 100007,
                    Title = "Town of Host: Enhanced v1.4.0",
                    SubTitle = "★★ Mises à jour consistantes, ouais ! ★★",
                    ShortTitle = "TOH: Enhanced v1.4.0",
                    BeforeNumber = 100006,
                    Text = "<size=150%>Bienvenue sur TOH: Enhanced v1.4.0!</size>\n" +
                    "\n<b>【Base】</b>\n - Basé sur TOH: Enhanced v1.3.0\r\n" +

                    "\n<b>【Nouveau Rôles/Modifieurs】(7 Rôles, 3 Modifieurs)</i></b>" +
                        "\n     - Bienfaiteur (Coéquipier de support - Par: ryuk)" +
                        "\n     - Protecteur (Coéquipier de support - Par: ryuk)" +
                        "\n     - Capitaine (Coéquipier de pouvoir - Par: ryuk)" +
                        "\n     - Taupe (Coéquipier Basique - Par: ryuk)" +
                        "\n     - Maître penseur (Coéquipier Basique - Par: ryuk)" +
                        "\n     - Kamikaze (Imposteur de support - Par: Drakos)" +
                        "\n     - Solsticer (Neutre éxperimental - Par: NikoCat223)" +
                        "\n     - Flash (Modifieur Utile - Par: NikoCat223)" +
                        "\n     - Discret (Modifieur Utile - Par: NikoCat223)" +
                        "\n     - Banal (Modifieur Nuisible - Par: ryuk)" +

                    "\n<b>【Nouveaux Paramètres】</b>" +
                        "\n     - dlekS ehT !etraC ellevuoN (Merci sleepyut (@Galster-dev sur GitHub) et TommyXL)" +
                        "\n     - Nouveau Mode de Jeu: FFA de TOHE+ (Par: ryuk, Remerciements Speciaux: Gurge44)" +
                        "\n     - Ajout des commandes de chat /tpin, /tpout - Téléporte les joueurs en dehors ou a l'intérieur du vaisseau dans le lobby (Par: ryuk)" +
                        "\n     - Nouveau Paramètre : empêcher la commande /quit en raison d'une utilisation malveillante (Par: Furo)" +
                        "\n     - Nouveau Paramètre : modifier le temps de décontamination (Très cool ! Essayez ceci! Par: TommyXL)" +
                        "\n     - Paramètre Réajouté : supprimer les animaux de compagnie des joueurs morts " +
                        "\n     - Nouvelle Région: Amérique du Sud Moddée - MSA (Par: Pietro)" +
                        "\n     - Nouvelle Région: Chine Moddée - Multiple (Par: NikoCat223)" +
                        "\n     - Nouveau Bouton: Mise à Jour! Maintenant, mettez à jour le mod automatiquement! (Par: Pietro)" +

                    "\n<b>【Changements】</b>" +
                        "\n     - Icônes de compétences ajoutées : Vautour, Poursuivant et Nettoyeur (Par: LeziYa)" +
                        "\n     - Lisibilité des logs mise à jour (Par: TommyXL)" +
                        "\n     - Enhanced Anti-Cheat (EAC) est maintenant gérée par l'API (Par: ryuk & Moe)" +
                        "\n     - Idiot (Modifieurs) est désormais incompatible avec Repairman (Modifieurs) pour éviter les problèmes (Par: ryuk)" +
                        "\n     - Le système envoie désormais un message après l'effacement - Très utile ! (Par: ryuk)" +

                    "\n<b>【Corrections de bugs】</b>" +
                        "\n     - Correction de charactères invalide pour l'Alchemist (Par: Drakos)" +
                        "\n     - Les votes s'annulent désormais si un joueur meurt au milieu du tour ou se déconnecte (Par: NikoCat223, ryuk)" +
                        "\n     - Plusieurs corrections de bugs (Par: NikoCat233, LezaiYa)" +
                        "\n     - Faute de frape dans l'Enigme (Par: Plaguer)" +
                        "\n     - Prevent-MM-Mass-Shapeshift - Définissez la notification du joueur tricheur sur \"Prevenir\" (Par: NikoCat223)" +
                        "\n     - Suppression des rôles inutiles dans les GUIs pour deviner (Par: NikoCat223)" +
                        "\n     - Correction du Workaholic obtenant Onbound, Rebound et Double Shot lorsque l'Anti-Guess est activé" +

                    "\n<b>【Credits des traducteurs】</b>" +
                        "\n     - Brésilien (Par: Dx7405)" +
                        "\n     - Néerlandais (Par: apemv, madmazel_)" +
                        "\n     - Français (Par: FuroYT, KevOut, Klaomi, Sansationnelle)" +
                        "\n     - Italien (Par: alot, Baphojack, Mattix606)" +
                        "\n     - Japonais (Par: Sunnyboi)" +
                        "\n     - Latino-Américain (Par: CreepPower)" +
                        "\n     - Russe (Par: chill_ultimated, Nevermore59, Shoulder Devil, TommyXL)" +
                        "\n     - Chinois simplifié (Par: CrewCyan, Hinharrrrr, LezaiYa, NikoCat223)" +
                        "\n     - Espagnol (Par: Dawson, Sunnyboi, thewhiskas27, xxSShadow)" +
                        "\n     - Chinois traditionnel (Par: FlyFlyTurtle, Hinharrrrr, netherdragontw, Pomelo_)" +
                        "\n<b> Découvrez tous nos traducteurs sur notre site</b>\r\n" +

                    "\n<b>【Plusieurs autres correctifs par les développeurs ci-dessus !】</b>\r\n" +


                    "\n\n★ Bienvenue sur Town of Host: Enhanced v1.4.0 ★",

                    Date = "2024-1-6T03:00:00Z"
                };
                AllModNews.Add(news);
            }
            {
                var news = new ModNews
                {
                    Number = 100006,
                    Title = "Town of Host: Enhanced v1.3.0",
                   SubTitle = "★★ Nouveau Rôles? Modifieurs? Correctifs de bugs?! ★★",
                    ShortTitle = "TOH: Enhanced v1.3.0",
                    BeforeNumber = 100005,
                    Text = "<size=150%>Bienvenue sur TOH: Enhanced v1.3.0!</size>\n" +
                       "\n<b>【Base】</b>\n - Basé sur TOH: Enhanced v1.2.0\r\n" +

                           "\n<b>【Nouveau Rôles/Modifieurs】(5 rôles, 3 Modifieurs)</i></b>" +
                            "\n     - Culpabiliseur (Imposteur tueur - Par: papercut)" +
                            "\n     - Énigme (Coéquipier de support - Par: papercut)" +
                            "\n     - Fée (Bénin Neutre - Par : ryuk, Idée : Azanthiel)" +
                            "\n     - Taskinateur (Bénin Neutre - Par : ryuk, Idée : Dx)" +
                            "\n     - Hasardeux (Coéquipier Basique - Idée et codé par : Night, Amélioré par : LezaiYa)" +
                            "\n     - Influencé (Modifieur Nuisible - Par: NikoCat223)" +
                            "\n     - Pressé (Modifieur Nuisible - Par: NikoCat223)" +
                            "\n     - Oiiai (Modifieur Experimental - Par: NikoCat223)\r\n" +

                       "\n<b>【Nouveaux Paramètres】</b>" +
                            "\n     - Ajout d'une fonctionnalité d'anti-spam pendant l'éjection (Par : Broke his pc's stupid, corrigé par : ryuk)" +
                            "\n     - Le rêveur converti peut tuer n'importe qui sans répercussions (Par: ryuk)" +
                            "\n     - Compte le temps pendant la réunion pour grandir (Pour le Mini) (Par: LezaiYa, NikoCat233)" +
                            "\n     - L'Égoïste compte comme un neutre converti (Par: ryuk)" +
                            "\n     - Nouveaux skins de camouflage ajoutés (Par: TommyXL)\r\n" +

                        "\n<b>【Changements】</b>" +
                            "\n     - Incompatibilité entre Cyber et Sosie (Par: ryuk)" +
                            "\n     - Système de bannissement amélioré (Par: NikoCat223)" +
                            "\n     - Base de code considérablement améliorée (Par: TommyXL)" +
                            "\n     - Paramètres de synchronisation améliorés (Par: TommyXL)" +
                            "\n     - Anti-spawn pour le Cauchemar et Harceleur sur The Fungle (Par: TommyXL)" +
                            "\n     - Attribution de rôle incompatible supprimée (Par: NikoCat223)" +
                            "\n     - Couleurs des rôles désormais modifiables via RoleColor.dat (Par: ryuk)" +
                            "\n     - Tags de dévelopeurs mis à jour (Par: FuroYT)" +
                            "\n     - Vecteur et Malchanceux ne sont plus compatibles (Par: ryuk)" +
                            "\n     - Le vautour ne peut plus manger les corps des nettoyés/méduses (Par: NikoCat223)\r\n" +

                        "\n<b>【Corrections de bugs】</b>" +
                            "\n     - Corrigé les raisons de morts incorrectes (Par: ryuk)" +
                            "\n     - Corrigé les double IDs pour les rôles qui en ont accès (Par: ryuk)" +
                            "\n     - <b>Corrigé /death de l'hôte qui s'affiche pour tous les joueurs (Par: NikoCat223)</b>" +
                            "\n     - Corrigé les options qui se réinitialisent ou écrasé\n<b>Vous devez jouer une partie ou deux</b> (Par: ryuk)" +
                            "\n     - Corrigé plein de rôles, Modifieurs, et bugs de téléportations (Par: NikoCat223, ryuk, TommyXL)\r\n" +

                        "\n<b>【Nouvelles langues】</b>" +
                            "\n     - Français (Par: FuroYT, KevOut, Klaomi, Sansationnelle)" +
                            "\n     - Japonais (Par: Sunnyboi)" +
                            "\n     - Latino-américain (Par: CreepPower)" +
                            "\n     - Italien (Par: alot, Baphojack, Mattix606)" +
                            "\n     - Espagnol (Par: xxSShadow)" +
                            "\n     - Chinois traditionnel (Par: FlyFlyTurtle, Pomelo_)" +
                            "\n<b> Découvrez tous nos traducteurs sur notre site Web</b>\r\n" +

                       "\n<b>【Plusieurs autres correctifs divers】</b>\r\n" +

                        "\n<b> Note: Les tags spéciaux, et les permissions se font maintenant en utilisant un bot Discord.</b>" +
                        "\nRejoignez le serveur Discord pour plus d'informations sur discord.gg/tohe" +
                        "\nAdditionnelement, toutes les options seront réinitialisées avec cette mise à jour. (Désolé, c'était néssaisaire)" +


                        "\n\n★ Bienvenue sur Town of Host: Enhanced v1.3.0 ★",

                    Date = "2023-12-2T03:00:00Z"
                };
                AllModNews.Add(news);
            }
            {
                var news = new ModNews
                {
                    Number = 60004,
                    Title = "Town of Host: Enhanced v1.2.0",
                    SubTitle = "★★ Support de \"The Fungle\"! ★★",
                    ShortTitle = "TOH: Enhanced v1.2.0",
                    BeforeNumber = 60003,
                    Text = "<size=150%>Bienvenue sur TOH: Enhanced v1.2.0!</size>\n" +
                    "\n<b>【Base】</b>\n - Basé sur TOH: Enhanced v1.1.1\r\n" +

                    "\n<b>【Changements】</b>" +
                        "\n     - Le support a été entièrement ajouté avec des bug corrigés pour la mise à jour \"The Fungle\"" +
                        "\n     - Ajouté une fonctionnalité de bannisement temporaire pour les joueurs qui quittent et rejoignent en permanence (troll)" +

                    "\n\n★ Bienvenue sur Town of Host: Enhanced: Edition Fungle ★",

                    Date = "2023-10-29T00:00:00Z"
                };
                AllModNews.Add(news);
            }
            {
                var news = new ModNews
                {
                    Number = 60003,
                    Title = "Town of Host: Enhanced v1.1.1",
                    SubTitle = "★★ Une mise à jour de performance avec des corrections de bugs! ★★",
                    ShortTitle = "TOH: Enhanced v1.1.1",
                    BeforeNumber = 60002,
                    Text = "<size=150%>Bienvenue sur TOH: Enhanced v1.1.1!</size>\n" +
                    "\n<b>【Base】</b>\n - Basé sur TOH: Enhanced v1.1.0\r\n" +

                    "\n<b>【Corrections de bugs】</b>" +
                        "\n     - Correction du (Evil) Mini ne pouvant pas être jugé ou vengé" +
                        "\n     - Correction de Swapper et de plusieurs bugs qu'il contient, restant expérimental" +
                        "\n     - Correction du Berserker incapable de mourir et des erreurs de spam" +
                        "\n     - Correction des problèmes de Mad Nice Mini" +
                        "\n     - Correction d'un conflit avec le Tiebreaker et le Void Ballot\n\r" +

                    "\n<b>【Autres correctifs】</b>" +
                        "\n     - Overkiller amélioré dans PlayerControls" +
                        "\n     - Retravaillé les contrôles de fin de partie (encore) pour l'optimiser" +

                    "\n\n★ Bienvenue sur Town of Host: Enhanced! ★",

                    Date = "2023-10-23T00:00:00Z"
                };
                AllModNews.Add(news);
            }
            {
                var news = new ModNews
                {
                    Number = 60002,
                    Title = "Town of Host: Enhanced v1.1.0",
                    SubTitle = "★★ Une mise a jour déjà?! Wow! ★★",
                    ShortTitle = "TOH: Enhanced v1.1.0",
                    BeforeNumber = 60001,
                    Text = "<size=150%>Bienvenue sur TOH: Enhanced v1.1.0!</size>\n" +
                    "\n<b>【Base】</b>\n - Basé sur TOH: Enhanced v1.0.1\r\n" +

                    "\n<b>【Nouveaux rôles/addons】</b>" +
                        "\n     - Président (Crewmate: Power)" +
                        "\n     - Spy (Crewmate: Support)" +
                        "\n     - Vigilante (Crewmate: Killing)\n\r" +

                        "\n     - Rebound (Addons: Mixed)\n\r" +

                    "\n<b>【Nouveaux Paramètres】</b>" +
                        "\n     - Crewpostor: Foncez ET tuer après chaque X tâches terminées" +
                        "\n     - Deathpact: Les joueurs du Deathpact peuvent faire une réunion" +
                        "\n     - Twister: Cacher avec les joueurs qui échangent\n" +

                        "\n     - Deceiver: Perd l'utilisation de sa capacité en cas de tromperie injustifiée" +
                        "\n     - Merchant: Ne peut vendre que des addons activés" +
                        "\n     - Coroner: Informer le tueur qu'il est suivi\n\r" +

                        "\n     - Infectious: Double-cliquez pour tuer/infecter" +

                        "\n     - Bewilder: Le tueur peut avoir la vision du Bewilder\n\r" +

                    "\n<b>【Rôles/addons supprimés】</b>" +
                        "\n     - Neutre: Occultist\n\r" +
                        "\n     - Addon: Sunglasses" +
                        "\n     - Addon: Glow" +

                    "\n<b>【Autres changemants】</b>" +
                        "\n     - Nouvelle langue prise en charge : portugais" +
                        "\n     - Nouveaux jeux de lobby : /rps & /coinflip" +
                        "\n     - Agent renommé en Evil Tracker" +
                        "\n     - Disruptor renommé en Anti Adminer" +
                        "\n     - Nouveaux skins de camouflage" +
                        "\n     - Nouveau Default_Template.txt" +
                        "\n     - Reverie ET Hater retravaillés" +

                    "\nPlusieurs autres changements de bugs (et quand je dis plusieurs, ca veux dire PLUSIEURS)" +

                    "\n\n★ Bienvenue sur Town of Host: Enhanced! ★",

                    Date = "2023-10-21T00:00:00Z"
                };
                AllModNews.Add(news);
            }
            {
                var news = new ModNews
                {
                    Number = 60001,
                    Title = "Town of Host: Enhanced v1.0.1",
                    SubTitle = "★★ Une nouvelle ère ★★",
                    ShortTitle = "TOH: Enhanced v1.0.1",
                    BeforeNumber = 60000,
                    Text = "<size=150%>Bienvenue sur TOH: Enhanced v1.0.1!</size>\n\n<size=125%>Support d'Among Us v2023.7.11 et v2023.7.12</size>\n" +
                    "\n<b>【Base】</b>\n - Basé sur TOH-RE v3.1.0 (Notes Disponibles)\r\n" +
                    "\n<b>【Correctifs】</b>" +
                    "\n     - Suppression du logo de Loonie et remplacement de celui-ci" +
                    "\n     - Ajout d'un indice mis à jour dans Fortune Teller" +
                    "\n     - Les modèles fixés et la liste VIP ne sont pas générés" +
                    "\n     - Un nouveau teaser... pour un nouveau rôle... ?" +
                    "\n\n★ Bienvenue sur Town of Host: Enhanced! ★",

                    Date = "2023-10-5T00:00:00Z"
                };
                AllModNews.Add(news);
            }
            {
                var news = new ModNews
                {
                    Number = 60000,
                    Title = "Town of Host: Enhanced v1.0.0",
                    SubTitle = "★★ A New Era ★★",
                    ShortTitle = "TOH: Enhanced v1.0.0",
                    Text = "<size=150%>Bienvenue sur TOH: Enhanced v1.0.0!</size>\n\n<size=125%>Support for Among Us v2023.7.11 and v2023.7.12</size>\n" +
                    "\n<b>【Base】</b>\n - Basé sur TOH-RE v3.1.0 (Notes Disponibles)\r\n" +
                    "\n<b>【Modifications/Corrections】</b>" +
                    "\n     - Suppression de toute association avec Loonie, crédit dans README" +
                    "\n     - Jailor renommé en Jailer (de rien, ryuk)" +
                    "\n     - Modèles mis à jour avec toutes les textes/variables" +
                    "\n     - Texte du Bandit corrigé" +
                    "\n\n★ Amélioration du mod. Bienvenue sur Town of Host: Enhanced! ★",

                    Date = "2023-10-5T00:00:00Z"
                };
                AllModNews.Add(news);
            }
        }
    }

    [HarmonyPatch(typeof(PlayerAnnouncementData), nameof(PlayerAnnouncementData.SetAnnouncements)), HarmonyPrefix]
    public static bool SetModAnnouncements_Prefix(PlayerAnnouncementData __instance, [HarmonyArgument(0)] ref Il2CppReferenceArray<Announcement> aRange)
    {
        if (AllModNews.Count == 0)
        {
            Init();
            AllModNews.Sort((a1, a2) => { return DateTime.Compare(DateTime.Parse(a2.Date), DateTime.Parse(a1.Date)); });
        }

        List<Announcement> FinalAllNews = new();
        AllModNews.Do(n => FinalAllNews.Add(n.ToAnnouncement()));
        foreach (var news in aRange.ToArray())
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

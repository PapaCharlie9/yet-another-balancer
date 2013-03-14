/* YetAnotherBalancer.cs

by PapaCharlie9@gmail.com

Free to use as is in any way you want with no warranty.

*/

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections;
using System.Net;
using System.Web;
using System.Data;
using System.Threading;
using System.Timers;
using System.Diagnostics;
using System.ComponentModel;
using System.Reflection;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;


namespace PRoConEvents
{

//Aliases
using EventType = PRoCon.Core.Events.EventType;
using CapturableEvent = PRoCon.Core.Events.CapturableEvents;

public class YetAnotherBalancer : PRoConPluginAPI, IPRoConPluginInterface
{

/* Inherited:
    this.PunkbusterPlayerInfoList = new Dictionary<String, CPunkbusterInfo>();
    this.FrostbitePlayerInfoList = new Dictionary<String, CPlayerInfo>();
*/

    public class PerModeSettings {
        public PerModeSettings() {}
        
        public double MinTicketsPercentage = 10.0;
        public int GoAggressive = 0;
    }

private bool fIsEnabled;
private Dictionary<String,String> fModeToSimple = null;
private Dictionary<int, Type> fEasyTypeDict = null;
private Dictionary<int, Type> fBoolDict = null;
private Dictionary<int, Type> fListStrDict = null;
private Dictionary<String,PerModeSettings> fPerMode = null;

private DateTime fLastPingTime = DateTime.Now;
private Object fPingLock = new Object();
private Thread fP1 = null;
private Thread fP2 = null;

/* Settings */

private int DebugLevel;
private bool QuietMode;
private String ShowInLog; // command line to show info in plugin.log

void pingLoop() {
    ConsoleWrite("pingLoop starting");
    while (fIsEnabled) {
        ServerCommand("version");
        Thread.Sleep(5*1000);
    }
    ConsoleWrite("pingLoop exiting");
}

void pingCheck() {
    ConsoleWrite("pingCheck starting");
    int maxBlast = 12;
    while (fIsEnabled) {
        DateTime check = DateTime.Now;
        Thread.Sleep(2500);
        lock (fPingLock) {
            check = fLastPingTime;
        }
        double n = DateTime.Now.Subtract(check).TotalSeconds;
        if (n > 10.0) {
            if (maxBlast > 0) {
                ConsoleWarn("^b^8 +-+-+-+-+-+-+ CHECK FOR LOCK-UP! +-+-+-+-+-+-+^0 " + n.ToString("F1") + " secs");
                maxBlast = maxBlast - 1;
                lock (fPingLock) {
                    fLastPingTime = DateTime.Now;
                }
            } else break;
        } else if (fSI != null) {
            DebugWrite("^9 ..... " + fSI.PlayerCount + " players  on " + fSI.Map + " for " + TimeSpan.FromSeconds(fSI.RoundTime).ToString(), 1);
            maxBlast = 12;
        }
    }
    ConsoleWrite("pingCheck exiting");
}

/* Constructor */

public YetAnotherBalancer() {
    /* Private members */
    fIsEnabled = false;
    fModeToSimple = new Dictionary<String,String>();

    fEasyTypeDict = new Dictionary<int, Type>();
    fEasyTypeDict.Add(0, typeof(int));
    fEasyTypeDict.Add(1, typeof(Int16));
    fEasyTypeDict.Add(2, typeof(Int32));
    fEasyTypeDict.Add(3, typeof(Int64));
    fEasyTypeDict.Add(4, typeof(float));
    fEasyTypeDict.Add(5, typeof(long));
    fEasyTypeDict.Add(6, typeof(String));
    fEasyTypeDict.Add(7, typeof(string));
    fEasyTypeDict.Add(8, typeof(double));

    fBoolDict = new Dictionary<int, Type>();
    fBoolDict.Add(0, typeof(Boolean));
    fBoolDict.Add(1, typeof(bool));

    fListStrDict = new Dictionary<int, Type>();
    fListStrDict.Add(0, typeof(List<String>));
    fListStrDict.Add(1, typeof(List<string>));
    
    fPerMode = new Dictionary<String,PerModeSettings>();
    
    fLastPingTime = DateTime.Now;
    
    /* Settings */
    
    DebugLevel = 2;
    QuietMode = false;
    ShowInLog = String.Empty;
}

/* Types */

public enum MessageType { Warning, Error, Exception, Normal };

/* Properties */

public String FormatMessage(String msg, MessageType type) {
    String prefix = "[^b" + GetPluginName() + "^n] ";

    if (type.Equals(MessageType.Warning))
        prefix += "^1^bWARNING^0^n: ";
    else if (type.Equals(MessageType.Error))
        prefix += "^1^bERROR^0^n: ";
    else if (type.Equals(MessageType.Exception))
        prefix += "^1^bEXCEPTION^0^n: ";

    return prefix + msg;
}


public void LogWrite(String msg)
{
    this.ExecuteCommand("procon.protected.pluginconsole.write", msg);
}

public void ConsoleWrite(String msg, MessageType type)
{
    LogWrite(FormatMessage(msg, type));
}

public void ConsoleWrite(String msg)
{
    ConsoleWrite(msg, MessageType.Normal);
}

public void ConsoleWarn(String msg)
{
    ConsoleWrite(msg, MessageType.Warning);
}

public void ConsoleError(String msg)
{
    ConsoleWrite(msg, MessageType.Error);
}

public void ConsoleException(String msg)
{
    ConsoleWrite(msg, MessageType.Exception);
}

public void DebugWrite(String msg, int level)
{
    if (DebugLevel >= level) ConsoleWrite(msg, MessageType.Normal);
}


public void ServerCommand(params String[] args)
{
    List<String> list = new List<String>();
    list.Add("procon.protected.send");
    list.AddRange(args);
    this.ExecuteCommand(list.ToArray());
}


public String GetPluginName() {
    return "Yet Another Balancer";
}

public String GetPluginVersion() {
    return "0.0.0.5";
}

public String GetPluginAuthor() {
    return "PapaCharlie9";
}

public String GetPluginWebsite() {
    return "TBD";
}

public String GetPluginDescription() {
    return @"
<h1>Yet Another Balancer</h1>
<p>For BF3, this plugin does live round team balancing and unstacking for all game modes, including Squad Deathmatch (SQDM).</p>

<h2>Description</h2>
<p>TBD</p>

<h2>Commands</h2>
<p>TBD</p>

<h2>Settings</h2>
<p>TBD</p>

<h2>Development</h2>
<p>TBD</p>
<h3>Changelog</h3>
<blockquote><h4>1.0.0.0 (10-JAN-2013)</h4>
    - initial version<br/>
</blockquote>
";
}




public List<CPluginVariable> GetDisplayPluginVariables() {


    List<CPluginVariable> lstReturn = new List<CPluginVariable>();

    try {
        lstReturn.Add(new CPluginVariable("1 - Settings|Debug Level", DebugLevel.GetType(), DebugLevel));

        lstReturn.Add(new CPluginVariable("1 - Settings|Quiet Mode", QuietMode.GetType(), QuietMode));

        List<String> simpleModes = GetSimplifiedModes();

        foreach (String sm in simpleModes) {
            PerModeSettings oneSet = null;
            if (!fPerMode.ContainsKey(sm)) {
                oneSet = new PerModeSettings();
                fPerMode[sm] = oneSet;
            } else {
                oneSet = fPerMode[sm];
            }

            lstReturn.Add(new CPluginVariable("3 - Settings for " + sm + "|" + sm + ": " + "Min Tickets Percentage", oneSet.MinTicketsPercentage.GetType(), oneSet.MinTicketsPercentage));

            lstReturn.Add(new CPluginVariable("3 - Settings for " + sm + "|" + sm + ": " + "Go Aggressive", oneSet.GoAggressive.GetType(), oneSet.GoAggressive));
        }

        lstReturn.Add(new CPluginVariable("9 - Debugging|Show In Log", ShowInLog.GetType(), ShowInLog));

        /*
        lstReturn.Add(new CPluginVariable("Game Settings|Zombie Mode Enabled", typeof(enumBoolYesNo), ZombieModeEnabled ? enumBoolYesNo.Yes : enumBoolYesNo.No));

        lstReturn.Add(new CPluginVariable("Admin Settings|Command Prefix", CommandPrefix.GetType(), CommandPrefix));

        lstReturn.Add(new CPluginVariable("Admin Settings|Announce Display Length", AnnounceDisplayLength.GetType(), AnnounceDisplayLength));

        lstReturn.Add(new CPluginVariable("Admin Settings|Warning Display Length", WarningDisplayLength.GetType(), WarningDisplayLength));

        lstReturn.Add(new CPluginVariable("Admin Settings|Human Max Idle Seconds", HumanMaxIdleSeconds.GetType(), HumanMaxIdleSeconds));

        lstReturn.Add(new CPluginVariable("Admin Settings|Max Idle Seconds", MaxIdleSeconds.GetType(), MaxIdleSeconds));

        lstReturn.Add(new CPluginVariable("Admin Settings|Warns Before Kick For Rules Violations", WarnsBeforeKickForRulesViolations.GetType(), WarnsBeforeKickForRulesViolations));

        lstReturn.Add(new CPluginVariable("Admin Settings|Temp Ban Instead Of Kick", typeof(enumBoolOnOff), TempBanInsteadOfKick ? enumBoolOnOff.On : enumBoolOnOff.Off));

        if (TempBanInsteadOfKick)
        {
            lstReturn.Add(new CPluginVariable("Admin Settings|Temp Ban Seconds", TempBanSeconds.GetType(), TempBanSeconds));
        }

        lstReturn.Add(new CPluginVariable("Admin Settings|Votes Needed To Kick", VotesNeededToKick.GetType(), VotesNeededToKick));

        lstReturn.Add(new CPluginVariable("Admin Settings|Debug Level", DebugLevel.GetType(), DebugLevel));

        lstReturn.Add(new CPluginVariable("Admin Settings|Rule List", typeof(string[]), RuleList.ToArray()));

        lstReturn.Add(new CPluginVariable("Admin Settings|Admin Users", typeof(string[]), AdminUsers.ToArray()));

        lstReturn.Add(new CPluginVariable("Game Settings|Max Players", MaxPlayers.GetType(), MaxPlayers));

        lstReturn.Add(new CPluginVariable("Game Settings|Minimum Zombies", MinimumZombies.GetType(), MinimumZombies));

        lstReturn.Add(new CPluginVariable("Game Settings|Minimum Humans", MinimumHumans.GetType(), MinimumHumans));

        lstReturn.Add(new CPluginVariable("Game Settings|Zombie Kill Limit Enabled", typeof(enumBoolOnOff), ZombieKillLimitEnabled ? enumBoolOnOff.On : enumBoolOnOff.Off));

        lstReturn.Add(new CPluginVariable("Game Settings|Deaths Needed To Be Infected", DeathsNeededToBeInfected.GetType(), DeathsNeededToBeInfected));

        lstReturn.Add(new CPluginVariable("Game Settings|Infect Suicides", typeof(enumBoolOnOff), InfectSuicides ? enumBoolOnOff.On : enumBoolOnOff.Off));

        lstReturn.Add(new CPluginVariable("Game Settings|New Players Join Humans", typeof(enumBoolOnOff), NewPlayersJoinHumans ? enumBoolOnOff.On : enumBoolOnOff.Off));

        lstReturn.Add(new CPluginVariable("Game Settings|Rematch Enabled", typeof(enumBoolOnOff), RematchEnabled ? enumBoolOnOff.On : enumBoolOnOff.Off));

        if (RematchEnabled)
        {
            lstReturn.Add(new CPluginVariable("Game Settings|Matches Before Next Map", MatchesBeforeNextMap.GetType(), MatchesBeforeNextMap));
        }


        if (ZombieKillLimitEnabled)
        {

            lstReturn.Add(new CPluginVariable("Goal For Humans|Kills If 8 Or Less Players", KillsIf8OrLessPlayers.GetType(), KillsIf8OrLessPlayers));

            lstReturn.Add(new CPluginVariable("Goal For Humans|Kills If 12 To 9 Players", KillsIf12To9Players.GetType(), KillsIf12To9Players));

            lstReturn.Add(new CPluginVariable("Goal For Humans|Kills If 16 To 13 Players", KillsIf16To13Players.GetType(), KillsIf16To13Players));

            lstReturn.Add(new CPluginVariable("Goal For Humans|Kills If 20 To 17 Players", KillsIf20To17Players.GetType(), KillsIf20To17Players));

            lstReturn.Add(new CPluginVariable("Goal For Humans|Kills If 24 To 21 Players", KillsIf24To21Players.GetType(), KillsIf24To21Players));

            lstReturn.Add(new CPluginVariable("Goal For Humans|Kills If 28 To 25 Players", KillsIf28To25Players.GetType(), KillsIf28To25Players));

            lstReturn.Add(new CPluginVariable("Goal For Humans|Kills If 32 To 29 Players", KillsIf32To29Players.GetType(), KillsIf32To29Players));
        }

        lstReturn.Add(new CPluginVariable("Human Damage Percentage|Against 1 Or 2 Zombies", Against1Or2Zombies.GetType(), Against1Or2Zombies));

        lstReturn.Add(new CPluginVariable("Human Damage Percentage|Against A Few Zombies", AgainstAFewZombies.GetType(), AgainstAFewZombies));

        lstReturn.Add(new CPluginVariable("Human Damage Percentage|Against Equal Numbers", AgainstEqualNumbers.GetType(), AgainstEqualNumbers));

        lstReturn.Add(new CPluginVariable("Human Damage Percentage|Against Many Zombies", AgainstManyZombies.GetType(), AgainstManyZombies));

        lstReturn.Add(new CPluginVariable("Human Damage Percentage|Against Countless Zombies", AgainstCountlessZombies.GetType(), AgainstCountlessZombies));

        foreach (PRoCon.Core.Players.Items.Weapon Weapon in WeaponDictionaryByLocalizedName.Values)
        {
            String WeaponDamage = Weapon.Damage.ToString();

            if (WeaponDamage.Equals("Nonlethal") || WeaponDamage.Equals("None") || WeaponDamage.Equals("Suicide"))
                continue;

            String WeaponName = Weapon.Name.ToString();
            lstReturn.Add(new CPluginVariable(String.Concat("Zombie Weapons|Z -", WeaponName), typeof(enumBoolOnOff), ZombieWeaponsEnabled.IndexOf(WeaponName) >= 0 ? enumBoolOnOff.On : enumBoolOnOff.Off));
            lstReturn.Add(new CPluginVariable(String.Concat("Human Weapons|H -", WeaponName), typeof(enumBoolOnOff), HumanWeaponsEnabled.IndexOf(WeaponName) >= 0 ? enumBoolOnOff.On : enumBoolOnOff.Off));
        }
        */

    } catch (Exception e) {
        ConsoleException(e.ToString());
    }

    return lstReturn;
}

public List<CPluginVariable> GetPluginVariables() {
    return GetDisplayPluginVariables();
}

public void SetPluginVariable(String strVariable, String strValue) {

    DebugWrite(strVariable + " <- " + strValue, 3);

    try {
        String tmp = strVariable;
        int pipeIndex = strVariable.IndexOf('|');
        if (pipeIndex >= 0) {
            pipeIndex++;
            tmp = strVariable.Substring(pipeIndex, strVariable.Length - pipeIndex);
        }

        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        String propertyName = tmp.Replace(" ", "");

        FieldInfo field = this.GetType().GetField(propertyName, flags);
        
        Type fieldType = null;


        if (!tmp.Contains("Settings for") && field != null) {
            fieldType = field.GetValue(this).GetType();
            if (fEasyTypeDict.ContainsValue(fieldType)) {
                field.SetValue(this, TypeDescriptor.GetConverter(fieldType).ConvertFromString(strValue));
            } else if (fListStrDict.ContainsValue(fieldType)) {
                field.SetValue(this, new List<string>(CPluginVariable.DecodeStringArray(strValue)));
            } else if (fBoolDict.ContainsValue(fieldType)) {
                DebugWrite(propertyName + " strValue = " + strValue, 3);
                if (Regex.Match(strValue, "True", RegexOptions.IgnoreCase).Success) {
                    field.SetValue(this, true);
                } else {
                    field.SetValue(this, false);
                }
            }
        } else {
            Match m = Regex.Match(tmp, @"([^:]+):\s([^:]+)$");
            
            if (m.Success) {
                String mode = m.Groups[1].Value;
                String perModeName = m.Groups[2].Value.Replace(" ","");
                
                if (!fPerMode.ContainsKey(mode)) {
                    fPerMode[mode] = new PerModeSettings();
                }
                PerModeSettings pms = fPerMode[mode];
                
                field = pms.GetType().GetField(perModeName, flags);
                
                if (field != null) {
                    fieldType = field.GetValue(pms).GetType();
                    if (fEasyTypeDict.ContainsValue(fieldType)) {
                        field.SetValue(pms, TypeDescriptor.GetConverter(fieldType).ConvertFromString(strValue));
                    } else if (fListStrDict.ContainsValue(fieldType)) {
                        field.SetValue(pms, new List<string>(CPluginVariable.DecodeStringArray(strValue)));
                    } else if (fBoolDict.ContainsValue(fieldType)) {
                        if (Regex.Match(strValue, "True", RegexOptions.IgnoreCase).Success) {
                            field.SetValue(pms, true);
                        } else {
                            field.SetValue(pms, false);
                        }
                    }
                } else {
                    DebugWrite("field is null", 3);
                }
                /*
                switch (perModeName) {
                    case "Min Tickets":
                        if (!Double.TryParse(strValue, out pms.MinTicketsPercentage)) {
                            ConsoleError("Bogus setting for " + strVariable + " ? " + strValue);
                        }
                        break;
                    case "Go Aggressive":
                        if (!Int32.TryParse(strValue, out pms.GoAggressive)) {
                            ConsoleError("Bogus setting for " + strVariable + " ? " + strValue);
                        }
                        break;
                }
                */
            }


            /*
            String WeaponName = strVariable.Substring(3, strVariable.Length - 3);

            if (WeaponList.IndexOf(WeaponName) >= 0)
            {
                String WeaponType = strVariable.Substring(0, 3);

                if (WeaponType == "H -")
                {
                    if (strValue == "On")
                        EnableHumanWeapon(WeaponName);
                    else
                        DisableHumanWeapon(WeaponName);
                }
                else
                {
                    if (strValue == "On")
                        EnableZombieWeapon(WeaponName);
                    else
                        DisableZombieWeapon(WeaponName);
                }

            }
            */
        }
    } catch (System.Exception e) {
        ConsoleException(e.ToString());
    } finally {
        // Validate all values and correct if needed
        
        if (!String.IsNullOrEmpty(ShowInLog)) {
            if (Regex.Match(ShowInLog, @"modes", RegexOptions.IgnoreCase).Success) {
                List<String> modeList = GetSimplifiedModes();
                DebugWrite("modes(" + modeList.Count + "):", 2);
                foreach (String m in modeList) {
                    DebugWrite(m, 2);
                }
            }
            
            ShowInLog = String.Empty;
        }
        
        /*
        if (DebugLevel < 0)
        {
            DebugValue("Debug Level", DebugLevel.ToString(), "must be greater than 0", "3");
            DebugLevel = 3; // default
        }
        if (String.IsNullOrEmpty(CommandPrefix))
        {
            DebugValue("Command Prefix", "(empty)", "must not be empty", "!zombie");
            CommandPrefix = "!zombie"; // default
        }
        if (AnnounceDisplayLength < 5 || AnnounceDisplayLength > 20)
        {
            DebugValue("Announce Display Length", AnnounceDisplayLength.ToString(), "must be between 5 and 20, inclusive", "10");
            AnnounceDisplayLength = 10; // default
        }
        if (WarningDisplayLength < 5 || WarningDisplayLength > 20)
        {
            DebugValue("Warning Display Length", WarningDisplayLength.ToString(), "must be between 5 and 20, inclusive", "15");
            WarningDisplayLength = 15; // default
        }
        if (MaxPlayers < 8 || MaxPlayers > 32)
        {
            DebugValue("Max Players", MaxPlayers.ToString(), "must be between 8 and 32, inclusive", "32");
            MaxPlayers = 32; // default
        }
        if (MinimumHumans < 2 || MinimumHumans > (MaxPlayers-1))
        {
            DebugValue("Minimum Humans", MinimumHumans.ToString(), "must be between 3 and " + (MaxPlayers-1), "2");
            MinimumHumans = 3; // default
        }
        if (MinimumZombies < 1 || MinimumZombies > (MaxPlayers-MinimumHumans))
        {
            DebugValue("Minimum Zombies", MinimumZombies.ToString(), "must be between 1 and " + (MaxPlayers-MinimumHumans), "1");
            MinimumZombies = 1; // default
        }
        if (DeathsNeededToBeInfected < 1 || DeathsNeededToBeInfected > 10)
        {
            DebugValue("Deaths Needed To Be Infected", DeathsNeededToBeInfected.ToString(), "must be between 1 and 10, inclusive", "1");
            DeathsNeededToBeInfected = 1; // default
        }
        if (HumanMaxIdleSeconds < 0 )
        {
            DebugValue("Human Max Idle Seconds", HumanMaxIdleSeconds.ToString(), "must not be negative", "120");
            HumanMaxIdleSeconds = 120; // default
        }
        if (MaxIdleSeconds < 0)
        {
            DebugValue("Max Idle Seconds", MaxIdleSeconds.ToString(), "must not be negative", "600");
            MaxIdleSeconds = 600; // default
        }
        if (KillsIf8OrLessPlayers < 6)
        {
            DebugValue("Kills If 8 Or Less Players", KillsIf8OrLessPlayers.ToString(), "must be 6 or more", "6");
            KillsIf8OrLessPlayers = 6; // default
        }
        */
    }

}


public void OnPluginLoaded(String strHostName, String strPort, String strPRoConVersion) {
    this.RegisterEvents(this.GetType().Name, 
    "OnVersion",
    "OnServerInfo",
    "OnResponseError",
    "OnListPlayers",
    "OnPlayerJoin",
    "OnPlayerLeft",
    "OnPlayerKilled",
    "OnPlayerSpawned",
    "OnPlayerTeamChange",
    "OnGlobalChat",
    "OnTeamChat",
    "OnSquadChat",
    "OnRoundOverPlayers",
    "OnRoundOver",
    "OnRoundOverTeamScores",
    "OnLoadingLevel",
    "OnLevelStarted",
    "OnLevelLoaded"
    );
}

public void OnPluginEnable() {
    fIsEnabled = true;
    ConsoleWrite("Enabled! Version = " + GetPluginVersion());
    
    Thread fP1 = new Thread(new ThreadStart(pingLoop));
    fP1.IsBackground = true;
    fP1.Name = "pingLoop";
    Thread fP2 = new Thread(new ThreadStart(pingCheck));
    fP2.IsBackground = true;
    fP2.Name = "pingCheck";
    
    fP1.Start();
    fP2.Start();
}


private void JoinWith(Thread thread, int secs)
{
    if (thread == null || !thread.IsAlive)
        return;

    ConsoleWrite("Waiting for ^b" + thread.Name + "^n to finish");
    thread.Join(secs*1000);
}


public void OnPluginDisable() {
    fIsEnabled = false;
    
    JoinWith(fP1, 6);
    JoinWith(fP2, 6);

    ConsoleWrite("Disabled!");
}


public override void OnVersion(String type, String ver) {
    lock (fPingLock) {
        fLastPingTime = DateTime.Now;
    }
    DebugWrite("OnVersion " + type + " " + ver, 9);
}

private CServerInfo fSI = null;

public override void OnServerInfo(CServerInfo serverInfo) {
    DebugWrite("Debug level = " + DebugLevel, 9);
    
    fSI = serverInfo;
}

public override void OnResponseError(List<String> requestWords, String error) { }

public override void OnListPlayers(List<CPlayerInfo> players, CPlayerSubset subset) {
}

public override void OnPlayerJoin(String soldierName) {
}

public override void OnPlayerLeft(CPlayerInfo playerInfo) {
}

public override void OnPlayerKilled(Kill kKillerVictimDetails) { }

public override void OnPlayerSpawned(String soldierName, Inventory spawnedInventory) { }

public override void OnPlayerTeamChange(String soldierName, int teamId, int squadId) { }

public override void OnGlobalChat(String speaker, String message) { }

public override void OnTeamChat(String speaker, String message, int teamId) { }

public override void OnSquadChat(String speaker, String message, int teamId, int squadId) { }

public override void OnRoundOverPlayers(List<CPlayerInfo> players) { }

public override void OnRoundOverTeamScores(List<TeamScore> teamScores) { }

public override void OnRoundOver(int winningTeamId) { }

public override void OnLoadingLevel(String mapFileName, int roundsPlayed, int roundsTotal) { }

public override void OnLevelStarted() { }

public override void OnLevelLoaded(String mapFileName, String Gamemode, int roundsPlayed, int roundsTotal) { } // BF3


/* ===== */

public List<String> GetSimplifiedModes() {
    List<String> r = new List<String>();
    
    if (fModeToSimple.Count < 1) {
        List<CMap> raw = this.GetMapDefines();
        foreach (CMap m in raw) {
            String simple = null;
            if (Regex.Match(m.GameMode, @"(?:Conquest|Assault)").Success) {
                simple = "Conquest";
            } else if (Regex.Match(m.GameMode, @"TDM").Success) {
                simple = "Team Deathmatch";
            } else if (Regex.Match(m.GameMode, @"Gun Master").Success) {
                continue; // not supported
            } else {
                simple = m.GameMode;
            }
            if (fModeToSimple.ContainsKey(m.PlayList)) {
                if (fModeToSimple[m.PlayList] != simple) {
                    ConsoleWarn("For mode " + m.PlayList + " old value " + fModeToSimple[m.PlayList] + " != new value " + simple);
                }
            } else {
                fModeToSimple[m.PlayList] = simple;
            }
        }
    }
    
    bool last = false;
    foreach (KeyValuePair<String,String> p in fModeToSimple) {
        if (r.Contains(p.Value)) continue;
        if (p.Value == "Squad Rush") { last = true; continue; }
        r.Add(p.Value); // collect up all the simple GameMode names
    }
    if (last) r.Add("Squad Rush"); // make sure this is last

    return r;
}


} // end YetAnotherBalancer

} // end namespace PRoConEvents




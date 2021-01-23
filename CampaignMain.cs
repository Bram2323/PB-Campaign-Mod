using System;
using System.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using HarmonyLib;
using Poly.Math;
using DCServices;
using Sirenix.Serialization;
using BepInEx;
using BepInEx.Configuration;
using PolyTechFramework;

namespace CampaignMod
{
    [BepInPlugin(pluginGuid, pluginName, pluginVerson)]
    [BepInProcess("Poly Bridge 2")]
    [BepInDependency(PolyTechMain.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(ConsoleMod.ConsoleMod.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
    public class CampaignMain : PolyTechMod
    {

        public const string pluginGuid = "polytech.campaignmod";

        public const string pluginName = "Campaign Mod";

        public const string pluginVerson = "1.0.0";

        public ConfigDefinition modEnableDef = new ConfigDefinition(pluginName, "Enable/Disable Mod");

        public ConfigDefinition NameDef = new ConfigDefinition(pluginName, "Name");

        public ConfigEntry<bool> mEnabled;

        public ConfigEntry<string> mName;

        public List<string> Keys = new List<string>();
        public bool LoadingCampaigns = false;

        public string MainPath = "";

        public static CampaignMain instance;

        void Awake()
        {
            if (instance == null) instance = this;
            MainPath = Application.dataPath.Replace("Poly Bridge 2_Data", "BepInEx/plugins/CampaignMod/");

            int order = 0;

            Config.Bind(modEnableDef, true, new ConfigDescription("Controls if the mod should be enabled or disabled", null, new ConfigurationManagerAttributes { Order = order }));
            mEnabled = (ConfigEntry<bool>)Config[modEnableDef];
            mEnabled.SettingChanged += onEnableDisable;
            order--;

            Config.Bind(NameDef, "", new ConfigDescription("Controls your name", null, new ConfigurationManagerAttributes { Order = order }));
            mName = (ConfigEntry<string>)Config[NameDef];
            order--;


            Config.SettingChanged += onSettingChanged;
            onSettingChanged(null, null);

            Harmony harmony = new Harmony(pluginGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            isCheat = false;
            isEnabled = mEnabled.Value;

            try
            {
                Debug.Log("Creatng folders");
                if (!Directory.Exists(MainPath))
                {
                    Directory.CreateDirectory(MainPath);
                    Debug.Log("CampaignMod folder Created!");
                }
                else
                {
                    Debug.Log("CampaignMod folder already exists!");
                }

                if (!Directory.Exists(MainPath + "Campaigns"))
                {
                    Directory.CreateDirectory(MainPath + "Campaigns");
                    Debug.Log("Campaigns folder Created!");
                }
                else
                {
                    Debug.Log("Campaigns folder already exists!");
                }

                if (!Directory.Exists(MainPath + "Exports"))
                {
                    Directory.CreateDirectory(MainPath + "Exports");
                    Debug.Log("Exports folder Created!");
                }
                else
                {
                    Debug.Log("Exports folder already exists!");
                }
            }
            catch(Exception e)
            {
                Debug.Log("Something went wrong while creating folders\n" + e);
            }

            PolyTechMain.registerMod(this);
        }

        public void onEnableDisable(object sender, EventArgs e)
        {
            isEnabled = mEnabled.Value;
        }

        public void onSettingChanged(object sender, EventArgs e)
        {

        }

        public override void enableMod()
        {
            this.isEnabled = true;
            mEnabled.Value = true;
            onEnableDisable(null, null);
        }

        public override void disableMod()
        {
            this.isEnabled = false;
            mEnabled.Value = false;
            onEnableDisable(null, null);
        }

        public override string getSettings()
        {
            return "";
        }

        public override void setSettings(string st)
        {
            return;
        }

        private bool CheckForCheating()
        {
            return mEnabled.Value && PolyTechMain.modEnabled.Value;
        }

        

        [HarmonyPatch(typeof(Main), "Update")]
        private static class patchUpdate
        {
            private static void Postfix()
            {
                if (!instance.CheckForCheating()) return;


            }
        }

        [HarmonyPatch(typeof(Main), "Start")]
        public class PatchStart
        {
            private static void Postfix()
            {
                uConsole.RegisterCommand("load_campaign", "loads a campaign", new uConsole.DebugCommand(instance.LoadCampaignCommand));
                uConsole.RegisterCommand("load_all_campaigns", "loads all the campaigns", new uConsole.DebugCommand(instance.LoadAllCampaignsCommand));
                uConsole.RegisterCommand("create_campaign", "creates an empty campaign", new uConsole.DebugCommand(instance.CreateCampaignCommand));
                uConsole.RegisterCommand("remove_campaign", "remove a campaign", new uConsole.DebugCommand(instance.RemoveCampaignCommand));
                uConsole.RegisterCommand("campaign_info", "shows some info of a campaign or a list of all campaigns", new uConsole.DebugCommand(instance.CampaignInfoCommand));
                uConsole.RegisterCommand("edit_campaign_info", "edit campaign info", new uConsole.DebugCommand(instance.EditCampaignInfoCommand));
                uConsole.RegisterCommand("remove_level", "removes a level from a campaign", new uConsole.DebugCommand(instance.RemoveLevelCommand));
                uConsole.RegisterCommand("add_this_level", "adds current sandbox level to a campaign", new uConsole.DebugCommand(instance.AddThisLevelCommand));
                uConsole.RegisterCommand("unload_campaign", "unloads a campaign", new uConsole.DebugCommand(instance.UnloadCampaignCommand));
                uConsole.RegisterCommand("export_campaign", "exports a campaign to a .campaign file", new uConsole.DebugCommand(instance.ExportCampaignCommand));
                uConsole.RegisterCommand("import_campaign", "imports a campaign from a .campaign file", new uConsole.DebugCommand(instance.ImportCampaignCommand));

                ResponseData.Campaign lol = new ResponseData.Campaign();
            }
        }


        //commands
        public void CampaignInfoCommand()
        {
            if (!instance.CheckForCheating())
            {
                uConsole.Log("Campaign Mod is not enabled!");
                return;
            }
            int Args = uConsole.GetNumParameters();
            if (Args == 0)
            {
                string[] dirs = Directory.GetDirectories(MainPath + "Campaigns");
                string Message = "Campaigns: ";
                bool first = true;
                foreach (string str in dirs)
                {
                    DirectoryInfo dirInf = new DirectoryInfo(str);
                    Message += (first ? "" : ", ") + dirInf.Name;
                    first = false;
                }
                uConsole.Log(Message);
            }
            else if (Args == 1)
            {
                string Name = uConsole.GetString();
                string cPath = MainPath + "Campaigns/" + Name;
                if (!Directory.Exists(cPath))
                {
                    uConsole.Log(Name + " does not exist!");
                    return;
                }
                else if (!File.Exists(cPath + "/CampaignData.json"))
                {
                    uConsole.Log(Name + " is missing CampaignData.json!");
                    return;
                }

                CampaignLayoutData cData = JsonUtility.FromJson<CampaignLayoutData>(File.ReadAllText(cPath + "/CampaignData.json"));

                string LevelNames = "";
                bool First = true;
                foreach (string str in cData.m_ItemIds)
                {
                    LevelNames += (First ? "" : ", ") + str.Replace("CampaignMod", "");
                    First = false;
                }

                uConsole.Log(
                    "<b>Title:</b> " + cData.m_Title +
                    "\n<b>Description:</b> " + cData.m_Description +
                    "\n<b>WinMessage:</b> " + cData.m_WinMessage +
                    "\n<b>Levels:</b> " + LevelNames
                    );
            }
            else
            {
                uConsole.Log("Usage: campaign_info [name]");
            }
        }
        
        public void EditCampaignInfoCommand()
        {
            if (!instance.CheckForCheating())
            {
                uConsole.Log("Campaign Mod is not enabled!");
                return;
            }
            int Args = uConsole.GetNumParameters();
            if (Args < 3)
            {
                uConsole.Log("Usage: edit_campaign_info <name> <title|description|winmessage> <value>");
            }
            else
            {
                string Name = uConsole.GetString();
                string Setting = uConsole.GetString();
                string Value = "";
                bool First = true;
                for (int i = 0; i < Args - 2; i++)
                {
                    Value += (First ? "" : " ") + uConsole.GetString();
                    First = false;
                }
                string cPath = MainPath + "Campaigns/" + Name;
                if (!Directory.Exists(cPath))
                {
                    uConsole.Log(Name + " does not exist!");
                    return;
                }
                else if (!File.Exists(cPath + "/CampaignData.json"))
                {
                    uConsole.Log(Name + " is missing CampaignData.json!");
                    return;
                }
                if (Value.Contains("/") || Value.Contains("\\") || Value.Contains("?") || Value.Contains("%") || Value.Contains("*")
                    || Value.Contains(":") || Value.Contains("|") || Value.Contains("\"") || Value.Contains("<") || Value.Contains(">")
                    || Value.Contains(".") || Value.Contains(",") || Value.Contains(";") || Value.Contains("="))
                {
                    uConsole.Log("Value contains prhobited characters!");
                    return;
                }

                CampaignLayoutData cData = JsonUtility.FromJson<CampaignLayoutData>(File.ReadAllText(cPath + "/CampaignData.json"));

                if (Setting == "title")
                {
                    if (Value.Contains(" "))
                    {
                        uConsole.Log("The campaign title cant contain a space!");
                        return;
                    }
                    else if (Directory.Exists(MainPath + "Campaigns/" + Value))
                    {
                        uConsole.Log("That campaign already exists!");
                        return;
                    }
                    Directory.Move(cPath, MainPath + "Campaigns/" + Value);
                    cPath = MainPath + "Campaigns/" + Value;
                    cData.m_Title = Value;
                    string json = JsonUtility.ToJson(cData);
                    File.WriteAllText(cPath + "/CampaignData.json", json);
                }
                else if (Setting == "description")
                {
                    cData.m_Description = Value;
                    string json = JsonUtility.ToJson(cData);
                    File.WriteAllText(cPath + "/CampaignData.json", json);
                }
                else if (Setting == "winmessage")
                {
                    cData.m_WinMessage = Value;
                    string json = JsonUtility.ToJson(cData);
                    File.WriteAllText(cPath + "/CampaignData.json", json);
                }
                else
                {
                    uConsole.Log("Usage: edit_campaign_info <name> <title|description|winmessage> <value>");
                    return;
                }
                uConsole.Log("Campaign info updated!");
            }
        }

        public void UnloadCampaignCommand()
        {
            if (!instance.CheckForCheating())
            {
                uConsole.Log("Campaign Mod is not enabled!");
                return;
            }
            int Args = uConsole.GetNumParameters();
            if (Args != 1)
            {
                uConsole.Log("Usage: unload_campaign <name>");
            }
            else
            {
                string Tag = uConsole.GetString() + "CampaignMod";
                if (!PersistentWorkshopCampaigns.Exists(Tag))
                {
                    uConsole.Log("That campaign is not loaded!");
                    return;
                }
                PersistentWorkshopCampaign campaign = PersistentWorkshopCampaigns.Get(Tag);
                
                foreach (ResponseData.Item item in campaign.definition.items)
                {
                    PersistentWorkshopItems.Delete(item.id);
                }
                PersistentWorkshopCampaigns.Delete(Tag);

                uConsole.Log("Unloaded " + Tag + "!");
            }
        }

        public void RemoveCampaignCommand()
        {
            if (!instance.CheckForCheating())
            {
                uConsole.Log("Campaign Mod is not enabled!");
                return;
            }
            int Args = uConsole.GetNumParameters();
            if (Args != 1)
            {
                uConsole.Log("Usage: remove_campaign <name>");
            }
            else
            {
                string Name = uConsole.GetString();
                string cPath = MainPath + "Campaigns/" + Name;
                if (!Directory.Exists(cPath))
                {
                    uConsole.Log(Name + " does not exist!");
                    return;
                }

                PopUpMessage.Display("Delete campaign " + Name + "?", delegate {
                    Directory.Delete(cPath, true);
                    uConsole.Log("Removed campaign " + Name);
                });
            }
        }

        public void CreateCampaignCommand()
        {
            if (!instance.CheckForCheating())
            {
                uConsole.Log("Campaign Mod is not enabled!");
                return;
            }
            int Args = uConsole.GetNumParameters();
            if (Args != 1)
            {
                uConsole.Log("Usage: create_campaign <name>");
            }
            else
            {
                string Name = uConsole.GetString();
                if (Name.Contains("/") || Name.Contains("\\") || Name.Contains("?") || Name.Contains("%") || Name.Contains("*") 
                    || Name.Contains(":") || Name.Contains("|") || Name.Contains("\"") || Name.Contains("<") || Name.Contains(">") 
                    || Name.Contains(".") || Name.Contains(",") || Name.Contains(";") || Name.Contains("=") || Name.Contains(" "))
                {
                    uConsole.Log(Name + " contains prhobited characters!");
                    return;
                }

                string cPath = MainPath + "Campaigns/" + Name;
                if (Directory.Exists(cPath))
                {
                    uConsole.Log(Name + " already exists!");
                    return;
                }
                else
                {
                    try
                    {
                        Directory.CreateDirectory(cPath);
                        CampaignLayoutData CampData = new CampaignLayoutData(1, "CampaignData", Name + "CampaignMod", Name, "", "", new List<string>());
                        string json = JsonUtility.ToJson(CampData);
                        File.WriteAllText(cPath + "/CampaignData.json", json);
                        uConsole.Log("Campaign created!");
                    }
                    catch (Exception e)
                    {
                        uConsole.Log("Something went wrong while creating campaign:\n" + e);
                        return;
                    }
                }
            }
        }
        
        public void RemoveLevelCommand()
        {
            if (!instance.CheckForCheating())
            {
                uConsole.Log("Campaign Mod is not enabled!");
                return;
            }
            int Args = uConsole.GetNumParameters();
            if (Args < 2)
            {
                uConsole.Log("Usage: remove_level <campaign> <level>");
            }
            else
            {
                string Name = uConsole.GetString();
                string Level = "";
                bool First = true;
                for (int i = 0; i < Args - 1; i++)
                {
                    Level += (First ? "" : " ") + uConsole.GetString();
                    First = false;
                }

                string cPath = MainPath + "Campaigns/" + Name;
                if (!Directory.Exists(cPath))
                {
                    uConsole.Log(Name + " does not exist!");
                    return;
                }
                else if (!File.Exists(cPath + "/CampaignData.json"))
                {
                    uConsole.Log(Name + " is missing CampaignData.json!");
                    return;
                }

                CampaignLayoutData cData = JsonUtility.FromJson<CampaignLayoutData>(File.ReadAllText(cPath + "/CampaignData.json"));

                if (!cData.m_ItemIds.Contains(Level + "CampaignMod"))
                {
                    uConsole.Log(Level + " is not in " + Name);
                    return;
                }

                if(File.Exists(cPath + "/" + Level + ".level")) File.Delete(cPath + "/" + Level + ".level");

                cData.m_ItemIds.Remove(Level + "CampaignMod");

                string json = JsonUtility.ToJson(cData);
                File.WriteAllText(cPath + "/CampaignData.json", json);

                uConsole.Log("Level removed from campaign");
            }
        }

        public void AddThisLevelCommand()
        {
            if (!instance.CheckForCheating())
            {
                uConsole.Log("Campaign Mod is not enabled!");
                return;
            }
            if (GameStateManager.GetState() != GameState.SANDBOX)
            {
                uConsole.Log("You have to be in sandbox mode to use this command!");
                return;
            }
            int Args = uConsole.GetNumParameters();
            if (Args == 0)
            {
                uConsole.Log("Usage: add_this_level <campaign> [num]");
            }
            else
            {
                string Name = uConsole.GetString();
                string cPath = MainPath + "Campaigns/" + Name;

                if (!Directory.Exists(cPath))
                {
                    uConsole.Log(Name + " does not exist!");
                    return;
                }
                else if (!File.Exists(cPath + "/CampaignData.json"))
                {
                    uConsole.Log(Name + " is missing CampaignData.json!");
                    return;
                }
                else if (mName.Value.IsNullOrWhiteSpace())
                {
                    uConsole.Log("Your name can't be empty!");
                    return;
                }

                GameUI.m_Instance.m_WorkshopSubmit.gameObject.SetActive(true);
                GameUI.m_Instance.m_WorkshopSubmit.gameObject.SetActive(false);

                SandboxLayoutData data = SandboxLayout.SerializeToProxies();
                string Title = data.m_Workshop.m_Title;
                if (Title.IsNullOrWhiteSpace())
                {
                    uConsole.Log("Level doesn't have a name!");
                    return;
                }
                else 
                if (Title.Contains("/") || Title.Contains("\\") || Title.Contains("?") || Title.Contains("%") || Title.Contains("*")
                    || Title.Contains(":") || Title.Contains("|") || Title.Contains("\"") || Title.Contains("<") || Title.Contains(">")
                    || Title.Contains(".") || Title.Contains(",") || Title.Contains(";") || Title.Contains("="))
                {
                    uConsole.Log("Level title contains prhobited characters!");
                    return;
                }

                CampaignLayoutData cData = JsonUtility.FromJson<CampaignLayoutData>(File.ReadAllText(cPath + "/CampaignData.json"));
                if (cData.m_ItemIds.Count >= 16)
                {
                    uConsole.Log("Campaign already has max levels");
                    return;
                }

                int num = -1;
                if (Args > 1)
                {
                    num = uConsole.GetInt();
                    if (num < 1 || num > 16)
                    {
                        uConsole.Log("Position has to be within 1 and 16!");
                        return;
                    }
                }

                if (File.Exists(cPath + "/" + Title + ".level"))
                {
                    PopUpMessage.Display("Level already exists!\nOverwrite level?", delegate { AddThisLevel(cPath, data, cData, num); });
                }
                else
                {
                    AddThisLevel(cPath, data, cData, num);
                }
            }
        }

        public void AddThisLevel (string cPath, SandboxLayoutData data, CampaignLayoutData cData, int num)
        {
            data.m_Workshop.m_Id = data.m_Workshop.m_Title + "CampaignMod";
            byte[] layoutData = data.SerializeBinary();

            List<byte> bytes = new List<byte>();

            WorkshopPreview.Create();
            bytes.AddRange(ByteSerializer.SerializeByteArray(layoutData));
            bytes.AddRange(ByteSerializer.SerializeByteArray(WorkshopPreview.m_PreviewBytes));
            bytes.AddRange(ByteSerializer.SerializeString(mName.Value));

            File.WriteAllBytes(cPath + "/" + data.m_Workshop.m_Title + ".level", bytes.ToArray());

            if (cData.m_ItemIds.Contains(data.m_Workshop.m_Id)) cData.m_ItemIds.Remove(data.m_Workshop.m_Id);
            if (num == -1)
            {
                cData.m_ItemIds.Add(data.m_Workshop.m_Id);
            }
            else
            {
                if (cData.m_ItemIds.Count < num) cData.m_ItemIds.Add(data.m_Workshop.m_Id);
                else
                {
                    cData.m_ItemIds.Insert(num - 1, data.m_Workshop.m_Id);
                }
            }

            try
            {
                string json = JsonUtility.ToJson(cData);
                File.WriteAllText(cPath + "/CampaignData.json", json);
            }
            catch (Exception e)
            {
                uConsole.Log("Something went wrong:\n" + e);
                return;
            }
            uConsole.Log("Level added to campaign at position " + (cData.m_ItemIds.IndexOf(data.m_Workshop.m_Id) + 1));
        }


        public void ExportCampaignCommand()
        {
            if (!instance.CheckForCheating())
            {
                uConsole.Log("Campaign Mod is not enabled!");
                return;
            }
            int Args = uConsole.GetNumParameters();
            if (Args != 1)
            {
                uConsole.Log("Usage: export_campaign <campaign>");
            }
            else
            {
                string Name = uConsole.GetString();
                string cPath = MainPath + "Campaigns/" + Name;
                if (!Directory.Exists(cPath))
                {
                    uConsole.Log(Name + " does not exist!");
                    return;
                }
                else if (!File.Exists(cPath + "/CampaignData.json"))
                {
                    uConsole.Log(Name + " is missing CampaignData.json!");
                    return;
                }

                List<byte> cBytes = new List<byte>();

                CampaignLayoutData cData = JsonUtility.FromJson<CampaignLayoutData>(File.ReadAllText(cPath + "/CampaignData.json"));

                cBytes.AddRange(ByteSerializer.SerializeString(File.ReadAllText(cPath + "/CampaignData.json")));

                List<LevelData> levelDatas = new List<LevelData>();
                foreach (string id in cData.m_ItemIds)
                {
                    if (!File.Exists(cPath + "/" + id.Replace("CampaignMod", "") + ".level"))
                    {
                        uConsole.Log(Name + " tried to export " + id.Replace("CampaignMod", "") + " but it doesn't exist!");
                        return;
                    }

                    cBytes.AddRange(ByteSerializer.SerializeString(id.Replace("CampaignMod", "")));
                    cBytes.AddRange(ByteSerializer.SerializeByteArray(File.ReadAllBytes(cPath + "/" + id.Replace("CampaignMod", "") + ".level")));
                }

                if (File.Exists(MainPath + "Exports/" + Name + ".campaign"))
                {
                    PopUpMessage.Display("an export of " + Name + "already exists\noverwrite it?", delegate {
                        File.WriteAllBytes(MainPath + "Exports/" + Name + ".campaign", Utils.ZipPayload(cBytes.ToArray()));
                        uConsole.Log("Export created at '" + MainPath + "Exports/" + Name + ".campaign'");
                    });
                }
                else
                {
                    File.WriteAllBytes(MainPath + "Exports/" + Name + ".campaign", Utils.ZipPayload(cBytes.ToArray()));
                    uConsole.Log("Export created at '" + MainPath + "Exports/" + Name + ".campaign'");
                }
            }
        }

        public void ImportCampaignCommand()
        {
            if (!instance.CheckForCheating())
            {
                uConsole.Log("Campaign Mod is not enabled!");
                return;
            }
            int Args = uConsole.GetNumParameters();
            if (Args != 1)
            {
                uConsole.Log("Usage: import_campaign <campaign>");
            }
            else
            {
                string Name = uConsole.GetString();
                string cPath = MainPath + "Campaigns/" + Name;

                if (!File.Exists(MainPath + "Exports/" + Name + ".campaign"))
                {
                    uConsole.Log("That campaign doesn't exist!");
                    return;
                }

                byte[] cBytes = Utils.UnZipPayload(File.ReadAllBytes(MainPath + "Exports/" + Name + ".campaign"));
                int offset = 0;

                string cDataJson = ByteSerializer.DeserializeString(cBytes, ref offset);
                CampaignLayoutData cData = JsonUtility.FromJson<CampaignLayoutData>(cDataJson);
                List<KeyValuePair<string, byte[]>> LevelBytes = new List<KeyValuePair<string, byte[]>>();

                for (int i = 0; i < cData.m_ItemIds.Count; i++)
                {
                    string lName = ByteSerializer.DeserializeString(cBytes, ref offset);
                    byte[] lData = ByteSerializer.DeserializeByteArray(cBytes, ref offset);
                    LevelBytes.Add(new KeyValuePair<string, byte[]>(lName, lData));
                }

                if (Directory.Exists(cPath))
                {
                    PopUpMessage.Display("Campaign already exists!\nOverwite it?", delegate {
                        Directory.Delete(cPath, true);
                        ImportCampaign(cPath, cDataJson, LevelBytes);
                    });
                }
                else
                {
                    ImportCampaign(cPath, cDataJson, LevelBytes);
                }
            }
        }
        public void ImportCampaign(string cPath, string cDataJson, List<KeyValuePair<string, byte[]>> LevelBytes)
        {
            Directory.CreateDirectory(cPath);

            File.WriteAllText(cPath + "/CampaignData.json", cDataJson);

            foreach (KeyValuePair<string, byte[]> pair in LevelBytes)
            {
                File.WriteAllBytes(cPath + "/" + pair.Key + ".level", pair.Value);
            }

            uConsole.Log("Campaign imported!");
        }


        public void LoadCampaignCommand()
        {
            if (!instance.CheckForCheating())
            {
                uConsole.Log("Campaign Mod is not enabled!");
                return;
            }
            int Args = uConsole.GetNumParameters();
            if (Args != 1)
            {
                uConsole.Log("Usage: load_campaign <name>");
            }
            else
            {
                string Name = uConsole.GetString();
                LoadCampaign(MainPath + "Campaigns/" + Name, Name);
                
                LoadingCampaigns = true;
                Panel_Workshop pWorkshop = GameUI.m_Instance.m_Workshop;
                pWorkshop.Open();
                pWorkshop.SelectTab(WorkshopTab.CLASSIC_CAMPAIGNS);
                pWorkshop.m_WorkshopCampaignItemPanel.gameObject.SetActive(false);
                Panel_WorkshopCampaign.m_ShowClassicCampaigns = true;
                pWorkshop.m_WorkshopCampaignPanel.Open();

                instance.Keys.Clear();
                instance.LoadingCampaigns = false;
            }
        }
        
        public void LoadAllCampaignsCommand()
        {
            if (!instance.CheckForCheating())
            {
                uConsole.Log("Campaign Mod is not enabled!");
                return;
            }
            string[] Campaigns = Directory.GetDirectories(MainPath + "Campaigns");
            foreach(string cPath in Campaigns)
            {
                LoadCampaign(cPath, new DirectoryInfo(cPath).Name);
            }

            LoadingCampaigns = true;
            Panel_Workshop pWorkshop = GameUI.m_Instance.m_Workshop;
            pWorkshop.Open();
            pWorkshop.SelectTab(WorkshopTab.CLASSIC_CAMPAIGNS);
            pWorkshop.m_WorkshopCampaignItemPanel.gameObject.SetActive(false);
            Panel_WorkshopCampaign.m_ShowClassicCampaigns = true;
            pWorkshop.m_WorkshopCampaignPanel.Open();

            instance.Keys.Clear();
            instance.LoadingCampaigns = false;
        }
        
        public void LoadCampaign(string cPath, string Name)
        {
            if (!Directory.Exists(cPath))
            {
                uConsole.Log(Name + " does not exist!");
                return;
            }
            else if (!File.Exists(cPath + "/CampaignData.json"))
            {
                uConsole.Log(Name + " is missing CampaignData.json!");
                return;
            }

            CampaignLayoutData cData = JsonUtility.FromJson<CampaignLayoutData>(File.ReadAllText(cPath + "/CampaignData.json"));
            ResponseData.Campaign campaign = GenerateCampaign(cPath, cData);
            if (campaign == null) return;
            Keys.Add(campaign.id);

            uConsole.Log(Name + " loaded!");
        }
        
        public ResponseData.Campaign GenerateCampaign(string cPath, CampaignLayoutData cData)
        {
            string cName = cData.m_Title;
            if (cData.m_ItemIds.Count == 0)
            {
                uConsole.Log(cName + " doesn't have any levels!");
                return null;
            }

            List<LevelData> levelDatas = new List<LevelData>();
            foreach (string id in cData.m_ItemIds)
            {
                if (!File.Exists(cPath + "/" + id.Replace("CampaignMod", "") + ".level"))
                {
                    uConsole.Log(cName + " tried to load " + id.Replace("CampaignMod", "") + " but it doesn't exist!");
                    return null;
                }
                byte[] bytes = File.ReadAllBytes(cPath + "/" + id.Replace("CampaignMod", "") + ".level");
                int offset = 0;
                levelDatas.Add(LevelData.Deserialize(bytes, ref offset));
            }
            
            List<ResponseData.Item> items = new List<ResponseData.Item>();
            foreach (LevelData levelData in levelDatas)
            {
                ResponseData.Item item = levelData.GenerateItem();
                PersistentWorkshopItems.Create(item, levelData.PreviewData, Utils.ZipPayload(levelData.LayoutBytes));

                items.Add(item);
            }

            ResponseData.Campaign campaign = new ResponseData.Campaign();

            campaign.ownedBy = items[0].ownedBy;
            campaign.items = items.ToArray();

            campaign.id = cName + "CampaignMod";
            campaign.title = cName;
            campaign.description = cData.m_Description;
            campaign.winMessage = cData.m_WinMessage;

            PersistentWorkshopCampaigns.Delete(campaign.id);
            PersistentWorkshopCampaigns.Create(campaign);
            return campaign;
        }


        [HarmonyPatch(typeof(WorkshopCampaignSlotGrid), "PopulateClassicCampaigns")]
        public class PatchPopulateClassicCampaigns
        {
            private static bool Prefix(WorkshopCampaignSlotGrid __instance)
            {
                if (!instance.CheckForCheating()) return true;
                if (!instance.LoadingCampaigns) return true;

                __instance.m_PendingSlots.Clear();
                __instance.DestroySlots();
                List<PersistentWorkshopCampaign> list = new List<PersistentWorkshopCampaign>();
                foreach (string key in instance.Keys)
                {
                    if (PersistentWorkshopCampaigns.m_Campaigns.ContainsKey(key))
                    {
                        list.Add(PersistentWorkshopCampaigns.m_Campaigns[key]);
                    }
                }
                foreach (PersistentWorkshopCampaign persistentWorkshopCampaign in list)
                {
                    __instance.CreatePendingSlot(persistentWorkshopCampaign.definition, __instance.m_ContentGrid);
                    __instance.m_TotalItems++;
                }
                __instance.m_ZeroItemsMessage.SetActive(__instance.m_TotalItems == 0);
                __instance.m_LoadingScreen.SetActive(false);
                __instance.m_ScrollbarHandle.SetActive(false);

                return false;
            }
        }
    }


    class LevelData
    {
        public static LevelData Deserialize(byte[] bytes, ref int offset)
        {
            LevelData levelData = new LevelData();
            
            levelData.LayoutBytes = ByteSerializer.DeserializeByteArray(bytes, ref offset);
            levelData.PreviewData = ByteSerializer.DeserializeByteArray(bytes, ref offset);
            levelData.PreviewTexture = new Texture2D(WorkshopPreview.PREVIEW_IMAGE_WIDTH, WorkshopPreview.PREVIEW_IMAGE_HEIGHT, TextureFormat.RGBA32, false);
            levelData.PreviewTexture.LoadImage(levelData.PreviewData);
            levelData.Name = ByteSerializer.DeserializeString(bytes, ref offset);

            int num = 0;
            levelData.LayoutData = DeserializeLayoutBytes(levelData.LayoutBytes, ref num);

            return levelData;
        }

        public ResponseData.Item GenerateItem()
        {
            ResponseData.Item item = new ResponseData.Item();

            item.ownedBy = new ResponseData.User();
            item.preview = new ResponseData.ItemPreview();
            item.metadata = new ResponseData.ItemMetadata();
            item.tags = new ResponseData.ItemTags();

            item.autoplay = LayoutData.m_Workshop.m_AutoPlay;
            item.description = LayoutData.m_Workshop.m_Description;
            item.id = LayoutData.m_Workshop.m_Title + "CampaignMod";
            item.leaderboardId = LayoutData.m_Workshop.m_LeaderboardId;
            item.m_PreviewData = PreviewData;
            item.m_PreviewTexture = PreviewTexture;
            item.ownedBy.displayName = Name;
            item.ownedBy.id = Name;
            item.tags.tags = LayoutData.m_Workshop.m_Tags.ToArray();
            item.title = LayoutData.m_Workshop.m_Title;

            return item;
        }

        public string Name;
        public byte[] PreviewData;
        public Texture2D PreviewTexture;
        public byte[] LayoutBytes;
        SandboxLayoutData LayoutData;
        

        public static SandboxLayoutData DeserializeLayoutBytes(byte[] bytes, ref int offset)
        {
            SandboxLayoutData layoutData = new SandboxLayoutData();
            layoutData.m_Version = ByteSerializer.DeserializeInt(bytes, ref offset);
            if (layoutData.m_Version < 0) layoutData.m_Version *= -1;
            layoutData.m_ThemeStubKey = ByteSerializer.DeserializeString(bytes, ref offset);
            if (layoutData.m_Version >= 19)
            {
                int num1 = ByteSerializer.DeserializeInt(bytes, ref offset);
                for (int i = 0; i < num1; i++)
                {
                    layoutData.m_Anchors.Add(new BridgeJointProxy(layoutData.m_Version, bytes, ref offset));
                }
            }
            if (layoutData.m_Version >= 5)
            {
                int num2 = ByteSerializer.DeserializeInt(bytes, ref offset);
                for (int i = 0; i < num2; i++)
                {
                    layoutData.m_HydraulicsPhases.Add(new HydraulicsPhaseProxy(bytes, ref offset));
                }
            }
            if (layoutData.m_Version > 4)
            {
                layoutData.m_Bridge.DeserializeBinary(bytes, ref offset);
                goto LABEL;
            }
            int num3 = ByteSerializer.DeserializeInt(bytes, ref offset);
            for (int i = 0; i < num3; i++)
            {
                layoutData.m_BridgeJoints.Add(new BridgeJointProxy(1, bytes, ref offset));
            }
            int num4 = ByteSerializer.DeserializeInt(bytes, ref offset);
            for (int i = 0; i < num4; i++)
            {
                layoutData.m_BridgeEdges.Add(new BridgeEdgeProxy(bytes, ref offset));
            }
            int num5 = ByteSerializer.DeserializeInt(bytes, ref offset);
            for (int i = 0; i < num5; i++)
            {
                layoutData.m_Pistons.Add(new PistonProxy(layoutData.m_Version, bytes, ref offset));
            }
            LABEL:
            if (layoutData.m_Version >= 7)
            {
                int num6 = ByteSerializer.DeserializeInt(bytes, ref offset);
                for (int i = 0; i < num6; i++)
                {
                    layoutData.m_ZedAxisVehicles.Add(new ZedAxisVehicleProxy(layoutData.m_Version, bytes, ref offset));
                }
            }
            int num7 = ByteSerializer.DeserializeInt(bytes, ref offset);
            for (int i = 0; i < num7; i++)
            {
                layoutData.m_Vehicles.Add(new VehicleProxy(bytes, ref offset));
            }
            int num8 = ByteSerializer.DeserializeInt(bytes, ref offset);
            for (int i = 0; i < num8; i++)
            {
                layoutData.m_VehicleStopTriggers.Add(new VehicleStopTriggerProxy(bytes, ref offset));
            }
            if (layoutData.m_Version < 20)
            {
                int num9 = ByteSerializer.DeserializeInt(bytes, ref offset);
                for (int i = 0; i < num9; i++)
                {
                    ByteSerializer.DeserializeVector2(bytes, ref offset);
                    ByteSerializer.DeserializeString(bytes, ref offset);
                    ByteSerializer.DeserializeBool(bytes, ref offset);
                }
            }
            int num10 = ByteSerializer.DeserializeInt(bytes, ref offset);
            for (int i = 0; i < num10; i++)
            {
                layoutData.m_EventTimelines.Add(new EventTimelineProxy(layoutData.m_Version, bytes, ref offset));
            }
            int num11 = ByteSerializer.DeserializeInt(bytes, ref offset);
            for (int i = 0; i < num11; i++)
            {
                layoutData.m_Checkpoints.Add(new CheckpointProxy(bytes, ref offset));
            }
            int num12 = ByteSerializer.DeserializeInt(bytes, ref offset);
            for (int i = 0; i < num12; i++)
            {
                layoutData.m_TerrainStretches.Add(new TerrainIslandProxy(layoutData.m_Version, bytes, ref offset));
            }
            int num13 = ByteSerializer.DeserializeInt(bytes, ref offset);
            for (int i = 0; i < num13; i++)
            {
                layoutData.m_Platforms.Add(new PlatformProxy(layoutData.m_Version, bytes, ref offset));
            }
            int num14 = ByteSerializer.DeserializeInt(bytes, ref offset);
            for (int i = 0; i < num14; i++)
            {
                layoutData.m_Ramps.Add(new RampProxy(layoutData.m_Version, bytes, ref offset));
            }
            if (layoutData.m_Version < 5)
            {
                int num15 = ByteSerializer.DeserializeInt(bytes, ref offset);
                for (int i = 0; i < num15; i++)
                {
                    layoutData.m_HydraulicsPhases.Add(new HydraulicsPhaseProxy(bytes, ref offset));
                }
            }
            int num16 = ByteSerializer.DeserializeInt(bytes, ref offset);
            for (int i = 0; i < num16; i++)
            {
                layoutData.m_VehicleRestartPhases.Add(new VehicleRestartPhaseProxy(bytes, ref offset));
            }
            int num17 = ByteSerializer.DeserializeInt(bytes, ref offset);
            for (int i = 0; i < num17; i++)
            {
                layoutData.m_FlyingObjects.Add(new FlyingObjectProxy(bytes, ref offset));
            }
            int num18 = ByteSerializer.DeserializeInt(bytes, ref offset);
            for (int i = 0; i < num18; i++)
            {
                layoutData.m_Rocks.Add(new RockProxy(bytes, ref offset));
            }
            int num19 = ByteSerializer.DeserializeInt(bytes, ref offset);
            for (int i = 0; i < num19; i++)
            {
                layoutData.m_WaterBlocks.Add(new WaterBlockProxy(layoutData.m_Version, bytes, ref offset));
            }
            if (layoutData.m_Version < 5)
            {
                int num20 = ByteSerializer.DeserializeInt(bytes, ref offset);
                for (int i = 0; i < num20; i++)
                {
                    ByteSerializer.DeserializeString(bytes, ref offset);
                    int num21 = ByteSerializer.DeserializeInt(bytes, ref offset);
                    for (int j = 0; j < num21; j++)
                    {
                        ByteSerializer.DeserializeString(bytes, ref offset);
                    }
                }
            }
            layoutData.m_Budget.DeserializeBinary(bytes, ref offset);
            layoutData.m_Settings.DeserializeBinary(bytes, ref offset);
            if (layoutData.m_Version >= 9)
            {
                int num22 = ByteSerializer.DeserializeInt(bytes, ref offset);
                for (int i = 0; i < num22; i++)
                {
                    layoutData.m_CustomShapes.Add(new CustomShapeProxy(layoutData.m_Version, bytes, ref offset));
                }
            }
            if (layoutData.m_Version >= 15)
            {
                layoutData.m_Workshop.DeserializeBinary(layoutData.m_Version, bytes, ref offset);
            }
            if (layoutData.m_Version >= 17)
            {
                int num23 = ByteSerializer.DeserializeInt(bytes, ref offset);
                for (int i = 0; i < num23; i++)
                {
                    layoutData.m_SupportPillars.Add(new SupportPillarProxy(bytes, ref offset));
                }
            }
            if (layoutData.m_Version >= 18)
            {
                int num24 = ByteSerializer.DeserializeInt(bytes, ref offset);
                for (int i = 0; i < num24; i++)
                {
                    layoutData.m_Pillars.Add(new PillarProxy(bytes, ref offset));
                }
            }
            return layoutData;
        }
    }



    /// <summary>
    /// Class that specifies how a setting should be displayed inside the ConfigurationManager settings window.
    /// 
    /// Usage:
    /// This class template has to be copied inside the plugin's project and referenced by its code directly.
    /// make a new instance, assign any fields that you want to override, and pass it as a tag for your setting.
    /// 
    /// If a field is null (default), it will be ignored and won't change how the setting is displayed.
    /// If a field is non-null (you assigned a value to it), it will override default behavior.
    /// </summary>
    /// 
    /// <example> 
    /// Here's an example of overriding order of settings and marking one of the settings as advanced:
    /// <code>
    /// // Override IsAdvanced and Order
    /// Config.AddSetting("X", "1", 1, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 3 }));
    /// // Override only Order, IsAdvanced stays as the default value assigned by ConfigManager
    /// Config.AddSetting("X", "2", 2, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 1 }));
    /// Config.AddSetting("X", "3", 3, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 2 }));
    /// </code>
    /// </example>
    /// 
    /// <remarks> 
    /// You can read more and see examples in the readme at https://github.com/BepInEx/BepInEx.ConfigurationManager
    /// You can optionally remove fields that you won't use from this class, it's the same as leaving them null.
    /// </remarks>
#pragma warning disable 0169, 0414, 0649
    internal sealed class ConfigurationManagerAttributes
    {
        /// <summary>
        /// Should the setting be shown as a percentage (only use with value range settings).
        /// </summary>
        public bool? ShowRangeAsPercent;

        /// <summary>
        /// Custom setting editor (OnGUI code that replaces the default editor provided by ConfigurationManager).
        /// See below for a deeper explanation. Using a custom drawer will cause many of the other fields to do nothing.
        /// </summary>
        public System.Action<BepInEx.Configuration.ConfigEntryBase> CustomDrawer;

        /// <summary>
        /// Show this setting in the settings screen at all? If false, don't show.
        /// </summary>
        public bool? Browsable;

        /// <summary>
        /// Category the setting is under. Null to be directly under the plugin.
        /// </summary>
        public string Category;

        /// <summary>
        /// If set, a "Default" button will be shown next to the setting to allow resetting to default.
        /// </summary>
        public object DefaultValue;

        /// <summary>
        /// Force the "Reset" button to not be displayed, even if a valid DefaultValue is available. 
        /// </summary>
        public bool? HideDefaultButton;

        /// <summary>
        /// Force the setting name to not be displayed. Should only be used with a <see cref="CustomDrawer"/> to get more space.
        /// Can be used together with <see cref="HideDefaultButton"/> to gain even more space.
        /// </summary>
        public bool? HideSettingName;

        /// <summary>
        /// Optional description shown when hovering over the setting.
        /// Not recommended, provide the description when creating the setting instead.
        /// </summary>
        public string Description;

        /// <summary>
        /// Name of the setting.
        /// </summary>
        public string DispName;

        /// <summary>
        /// Order of the setting on the settings list relative to other settings in a category.
        /// 0 by default, higher number is higher on the list.
        /// </summary>
        public int? Order;

        /// <summary>
        /// Only show the value, don't allow editing it.
        /// </summary>
        public bool? ReadOnly;

        /// <summary>
        /// If true, don't show the setting by default. User has to turn on showing advanced settings or search for it.
        /// </summary>
        public bool? IsAdvanced;

        /// <summary>
        /// Custom converter from setting type to string for the built-in editor textboxes.
        /// </summary>
        public System.Func<object, string> ObjToStr;

        /// <summary>
        /// Custom converter from string to setting type for the built-in editor textboxes.
        /// </summary>
        public System.Func<string, object> StrToObj;
    }
}

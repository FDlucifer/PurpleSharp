﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using PurpleSharp.Lib;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Management.Automation.Language;

namespace PurpleSharp
{
    class Program
    {

        static void Usage()
        {
            //slant

            string banner = @"
        ____                   __    _____ __                   
       / __ \__  ___________  / /__ / ___// /_  ____ __________ 
      / /_/ / / / / ___/ __ \/ / _ \\__ \/ __ \/ __ `/ ___/ __ \
     / ____/ /_/ / /  / /_/ / /  __/__/ / / / / /_/ / /  / /_/ /
    /_/    \__,_/_/  / .___/_/\___/____/_/ /_/\__,_/_/  / .___/ 
                    /_/                                /_/      
            ";
            Console.WriteLine(banner);
            Console.WriteLine("\t\t\tby Mauricio Velazco (@mvelazco)");
            Console.WriteLine();
            Console.WriteLine("\t\t\thttps://github.com/mvelazc0/PurpleSharp");
            Console.WriteLine("\t\t\thttps://purplesharp.readthedocs.io");
            Console.WriteLine();
        }



        public static void Main(string[] args)
        {
            bool cleanup, opsec, verbose, scoutservice, simservice, newchild, scout, remote, navigator;
            string techniques, rhost, domain, ruser, rpwd, scoutfpath, simrpath, log, dc, pb_file, nav_action, navfile, scout_action, scout_np, simulator_np;
            int pbsleep, tsleep, nusers, nhosts, variation;
            variation = 1;
            pbsleep = tsleep = 0;
            nusers = nhosts = 7;
            opsec = cleanup = true;
            verbose = scoutservice = simservice = newchild = scout = remote = navigator = false; 
            techniques = rhost = domain = ruser = rpwd = dc = pb_file = nav_action = navfile = scout_action = "";
            CommandlineParameters config_params=new CommandlineParameters();

            scoutfpath = "C:\\Windows\\Temp\\Scout.exe";
            simrpath = "Downloads\\Firefox_Installer.exe";
            log = "0001.dat";
            scout_np = "scoutpipe";
            simulator_np="simpipe";

            //should move this to sqlite or a JSON file.
            string[] execution = new string[] { "T1053.005", "T1059.003", "T1059.005", "T1059.007", "T1059.001", "T1569.002"};
            string[] persistence = new string[] { "T1053.005", "T1136.001", "T1543.003", "T1547.001", "T1546.003", "T1197" };
            string[] privelege_escalation = new string[] { "T1053.005", "T1543.003", "T1547.001", "T1546.003", "T1055.002", "T1055.004" };
            string[] defense_evasion = new string[] { "T1218.010", "T1218.005", "T1218.003", "T1218.011", "T1070.001", "T1220", "T1055.002", "T1055.003", "T1055.004", "T1140", "T1197", "T1218.009", "T1218.004", "T1134.004" };
            string[] credential_access = new string[] { "T1110.003", "T1558.003", "T1003.001" };
            string[] discovery = new string[] { "T1135", "T1046", "T1087.001", "T1087.002", "T1007", "T1033", "T1049", "T1016", "T1083", "T1482", "T1201","T1069.001", "T1069.002", "T1012", "T1518.001", "T1082", "T1124" };
            string[] lateral_movement = new string[] { "T1021", "T1021.006", "T1047" };
            string[] supported_techniques = execution.Union(persistence).Union(privelege_escalation).Union(defense_evasion).Union(credential_access).Union(discovery).Union(lateral_movement).ToArray();

            if (args.Length == 0)
            {
                Usage();
                return;
            }

            for (int i = 0; i < args.Length; i++)
            {
                try
                {
                    switch (args[i])
                    {
                        //// User Parameters ////
                        case "/pb":
                            pb_file = args[i + 1];
                            break;
                        case "/rhost":
                            rhost = args[i + 1];
                            remote = true;
                            break;
                        case "/ruser":
                            ruser = args[i + 1];
                            break;
                        case "/d":
                            domain = args[i + 1];
                            break;
                        case "/rpwd":
                            rpwd = args[i + 1];
                            break;
                        case "/dc":
                            dc = args[i + 1];
                            break;
                        case "/t":
                            techniques = args[i + 1];
                            break;
                        case "/scoutpath":
                            scoutfpath = args[i + 1];
                            break;
                        case "/simpath":
                            simrpath = args[i + 1];
                            break;
                        case "/pbsleep":
                            pbsleep = Int32.Parse(args[i + 1]);
                            break;
                        case "/tsleep":
                            tsleep = Int32.Parse(args[i + 1]);
                            break;
                        case "/var":
                            variation = Int32.Parse(args[i + 1]);
                            break;
                        case "/noopsec":
                            opsec = false;
                            break;
                        case "/v":
                            verbose = true;
                            break;
                        case "/nocleanup":
                            cleanup = false;
                            break;
                        case "/scout":
                            scout = true;
                            scout_action = args[i + 1];
                            break;
                        case "/navigator":
                            navigator = true;
                            nav_action = args[i + 1];
                            if (nav_action.Equals("import")) navfile = args[i + 2];
                            break;

                        //// Internal Parameters ////
                        case "/o":
                            scoutservice = true;
                            break;
                        case "/s":
                            simservice = true;
                            break;
                        case "/n":
                            newchild = true;
                            break;
                        default:
                            break;
                    }

                }
                catch
                {
                    Console.WriteLine("[*] Error parsing parameters :( ");
                    Console.WriteLine("[*] Exiting");
                    return;
                }

            }

            config_params = new CommandlineParameters(scoutfpath, simrpath, rhost, ruser, rpwd, domain, techniques, dc, scout_np, simulator_np, log, variation, pbsleep, tsleep, cleanup, opsec, verbose);
            //// Handling Internal Parameters ////

            if (newchild)
            {
                const uint NORMAL_PRIORITY_CLASS = 0x0020;
                Structs.PROCESS_INFORMATION pInfo = new Structs.PROCESS_INFORMATION();
                Structs.STARTUPINFO sInfo = new Structs.STARTUPINFO();
                Structs.SECURITY_ATTRIBUTES pSec = new Structs.SECURITY_ATTRIBUTES();
                Structs.SECURITY_ATTRIBUTES tSec = new Structs.SECURITY_ATTRIBUTES();
                pSec.nLength = Marshal.SizeOf(pSec);
                tSec.nLength = Marshal.SizeOf(tSec);
                string currentbin = System.Reflection.Assembly.GetEntryAssembly().Location;
                //run the Simulator
                WinAPI.CreateProcess(null, currentbin + " /s", ref pSec, ref tSec, false, NORMAL_PRIORITY_CLASS, IntPtr.Zero, null, ref sInfo, out pInfo);
                return;
            }
            if (scoutservice)
            {
                //NamedPipes.RunScoutService(scout_np, simulator_np, log);
                NamedPipes.RunScoutServiceSerialized(scout_np, simulator_np, log);
                return;
            }
            if (simservice)
            {
                SimulationPlaybook playbook = NamedPipes.RunSimulationServiceSerialized(simulator_np, log);
                ExecutePlaybook(playbook, log);
                return;
            }

            //// Handling  User Parameters ////

            //ATT&CK Navigator optionsE
            if (navigator)
            {

                if (nav_action.Equals("export"))
                {
                    try
                    {
                        Console.WriteLine("[+] PurpleSharp supports "+ supported_techniques.Count() +" unique ATT&CK techniques.");
                        Console.WriteLine("[+] Generating an ATT&CK Navigator layer...");
                        Json.ExportAttackLayer(supported_techniques.Distinct().ToArray());
                        Console.WriteLine("[!] Open PurpleSharp_Navigator.json on https://mitre-attack.github.io/attack-navigator");
                        return;
                    }
                    catch
                    {
                        Console.WriteLine("[!] Error generating JSON layer...");
                        Console.WriteLine("[!] Exitting...");
                        return;
                    }
                }
                else if (nav_action.Equals("import"))
                {
                    Console.WriteLine("[+] Loading {0}", navfile);
                    string json = File.ReadAllText(navfile);
                    NavigatorLayer layer = Json.ReadNavigatorLayer(json);
                    Console.WriteLine("[!] Loaded attack navigator '{0}'", layer.name);
                    Console.WriteLine("[+] Converting ATT&CK navigator Json...");
                    SimulationExercise engagement = Json.ConvertNavigatorToSimulationExercise(layer, supported_techniques.Distinct().ToArray());
                    Json.CreateSimulationExercise(engagement);
                    Console.WriteLine("[!] Done");
                    Console.WriteLine("[+] Open simulation.json");
                    return;
                }
                else
                {
                    Console.WriteLine("[!] Didnt recognize parameter...");
                    Console.WriteLine("[!] Exitting...");
                    return;
                }

            }

            //Scout: remote enumeration options
            if (scout && !scout_action.Equals(""))
            {
                string currentPath = AppDomain.CurrentDomain.BaseDirectory;
                Lib.Logger logger = new Lib.Logger(currentPath + log);

                if (!rhost.Equals("") && !domain.Equals("") && !ruser.Equals(""))
                {
                    if (rpwd == "")
                    {
                        Console.Write("Password for {0}\\{1}: ", domain, ruser);
                        rpwd = Utils.GetPassword();
                        Console.WriteLine();
                    }

                    if (!rhost.Equals("random"))
                    {
                        Scout(rhost, domain, ruser, rpwd, scoutfpath, log, scout_action, scout_np, verbose);
                        return;
                    }
                    else if (!dc.Equals(""))
                    {
                        List<Computer> targets = new List<Computer>();
                        //targets = Ldap.GetADComputers(10, dc, ruser, rpwd);
                        targets = Ldap.GetADComputers(10, logger, dc, ruser, rpwd);
                        if (targets.Count > 0)
                        {
                            Console.WriteLine("[+] Obtained {0} possible targets.", targets.Count);
                            var random = new Random();
                            int index = random.Next(targets.Count);
                            Console.WriteLine("[+] Picked Random host for simulation: " + targets[index].Fqdn);
                            Scout(targets[index].ComputerName, domain, ruser, rpwd, scoutfpath, log, scout_action, scout_np, verbose);
                            return;
                        }
                        else
                        {
                            Console.WriteLine("[!] Could not obtain targets for the simulation");
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine("[*] Missing parameters :( ");
                        Console.WriteLine("[*] Exiting");
                        return;
                    }
                }
                else
                {
                    Console.WriteLine("[*] Missing parameters :( ");
                    Console.WriteLine("[*] Exiting");
                    return;
                }
            }

            //JSON playbook handling
            if (!pb_file.Equals(""))
            {
                string json = File.ReadAllText(pb_file);
                SimulationExercise engagement = Json.ReadSimulationPlaybook(json);
                string currentPath = AppDomain.CurrentDomain.BaseDirectory;
                Lib.Logger logger = new Lib.Logger(currentPath + log);

                if (engagement != null)
                {
                    if (engagement.type.Equals("local"))
                    {

                        string results="";
                        foreach (SimulationPlaybook playbook in engagement.playbooks)
                        {
                            SimulationPlaybookResult playbookResults = new SimulationPlaybookResult();
                            playbookResults.taskresults = new List<PlaybookTaskResult>();
                            playbookResults.name = playbook.name;
                            playbookResults.host = playbook.remote_host;
                            logger.TimestampInfo("Running Playbook " + playbook.name);
                            PlaybookTask lastTask = playbook.tasks.Last();
                            foreach (PlaybookTask task in playbook.tasks)
                            {
                                ExecutePlaybookTechnique(task, log);
                                if (playbook.playbook_sleep > 0 && task != lastTask) Thread.Sleep(1000 * playbook.playbook_sleep);
                            }
                            logger.TimestampInfo("Playbook Finished");
                        }
                        results = System.IO.File.ReadAllText(log);
                        string output_file = pb_file.Replace(".json", "") + "_results.json";
                        SimulationExerciseResult simulationresults = Json.GetSimulationExerciseResult(results);
                        Json.WriteJsonSimulationResults(simulationresults, output_file);
                    }

                    else if (engagement.type.Equals("remote"))
                    { 
                        Console.Write("Submit Password for {0}\\{1}: ", engagement.domain, engagement.username);
                        string pass = Utils.GetPassword();
                        Console.WriteLine("[+] PurpleSharp will execute {0} playbook(s)", engagement.playbooks.Count);

                        SimulationExerciseResult engagementResults = new SimulationExerciseResult();
                        engagementResults.playbookresults = new List<SimulationPlaybookResult>();
                        SimulationPlaybook lastPlaybook = engagement.playbooks.Last();

                        foreach (SimulationPlaybook playbook in engagement.playbooks)
                        {
                            SimulationPlaybookResult playbookResults = new SimulationPlaybookResult();
                            playbookResults.taskresults = new List<PlaybookTaskResult>();
                            playbookResults.name = playbook.name;
                            playbookResults.host = playbook.remote_host;
                            Console.WriteLine("[+] Running Playbook {0}", playbook.name);

                            PlaybookTask lastTask = playbook.tasks.Last();
                            List<string> techs = new List<string>();
                            foreach (PlaybookTask task in playbook.tasks)
                            {
                                techs.Add(task.technique);
                            }
                            string techs2 = String.Join(",", techs);
                            if (playbook.remote_host.Equals("random"))
                            {
                                List<Computer> targets = Ldap.GetADComputers(10, logger, engagement.dc, engagement.username, pass);
                                if (targets.Count > 0)
                                {
                                    Console.WriteLine("[+] Obtained {0} possible targets.", targets.Count);
                                    var random = new Random();
                                    int index = random.Next(targets.Count);
                                    Console.WriteLine("[+] Picked random host for simulation: " + targets[index].Fqdn);
                                    Console.WriteLine("[+] Executing techniques {0} against {1}", techs2, targets[index].Fqdn);
                                    playbookResults = ExecuteRemoteTechniquesJsonSerialized(playbook.remote_host, engagement.domain, engagement.username, pass, scout_np, playbook, log, false);
                                    if (playbookResults == null) continue;
                                    playbookResults.name = playbook.name;
                                }
                                else Console.WriteLine("[!] Could not obtain targets for the simulation");

                            }
                            else
                            {
                                Console.WriteLine("[+] Executing techniques {0} against {1}", techs2, playbook.remote_host);
                                playbookResults = ExecuteRemoteTechniquesJsonSerialized(playbook.remote_host, engagement.domain, engagement.username, pass,scout_np, playbook, log, false);
                                if (playbookResults == null) continue;
                                playbookResults.name = playbook.name;
                            }
                            if (engagement.sleep > 0 && !playbook.Equals(lastPlaybook))
                            {
                                Console.WriteLine();
                                Console.WriteLine("[+] Sleeping {0} minutes until next playbook...", engagement.sleep);
                                Thread.Sleep(1000 * engagement.sleep * 60);
                            }
                            engagementResults.playbookresults.Add(playbookResults);
                        }

                        Console.WriteLine("Writting JSON results...");
                        string output_file = pb_file.Replace(".json", "") + "_results.json";
                        Json.WriteJsonSimulationResults(engagementResults, output_file);
                        Console.WriteLine("DONE. Open " + output_file);
                        Console.WriteLine();
                        return;
                    }

                }
                else
                {
                    Console.WriteLine("[!] Could not parse JSON input.");
                    Console.WriteLine("[*] Exiting");
                    return;
                }   
            }

            //Remote simulations with command line parameters
            if (remote)
            {
                string currentPath = AppDomain.CurrentDomain.BaseDirectory;
                Lib.Logger logger = new Lib.Logger(currentPath + log);

                if (!rhost.Equals("") && !domain.Equals("") && !ruser.Equals("") && !techniques.Equals(""))
                {
                    if (rpwd == "")
                    {
                        Console.Write("Password for {0}\\{1}: ", domain, ruser);
                        rpwd = Utils.GetPassword();
                        config_params.remote_password = rpwd;
                        Console.WriteLine();
                    }
                    if (!rhost.Equals("random"))
                    {
                        ExecuteRemoteTechniquesSerialized(config_params);
                        //ExecuteRemoteTechniquesSerialized(rhost, domain, ruser, rpwd, techniques, variation, pbsleep, tsleep, scoutfpath, scout_np, simrpath, simulator_np, log, opsec, verbose, cleanup);
                        return;
                    }
                    else if (!dc.Equals(""))
                    {
                        List<Computer> targets = new List<Computer>();
                        //targets = Ldap.GetADComputers(10, dc, ruser, rpwd);
                        targets = Ldap.GetADComputers(10, logger, dc, ruser, rpwd);
                        if (targets.Count > 0)
                        {
                            Console.WriteLine("[+] Obtained {0} possible targets.", targets.Count);
                            var random = new Random();
                            int index = random.Next(targets.Count);
                            Console.WriteLine("[+] Picked Random host for simulation: " + targets[index].Fqdn);
                            config_params.remote_host = targets[index].Fqdn;
                            ExecuteRemoteTechniquesSerialized(config_params);
                            //ExecuteRemoteTechniquesSerialized(targets[index].Fqdn, domain, ruser, rpwd, techniques, variation, pbsleep, tsleep, scoutfpath, scout_np, simrpath, simulator_np, log, opsec, verbose, cleanup);
                            return;
                        }
                        else
                        {
                            Console.WriteLine("[!] Could not obtain targets for the simulation");
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine("[*] Missing dc :( ");
                        Console.WriteLine("[*] Exiting");
                        return;
                    }

                }
                else
                {
                    Console.WriteLine("[*] Missing parameters :( ");
                    Console.WriteLine("[*] Exiting");
                    return;
                }
            }
            
            //Local simulations with command line parameters
            else if (!techniques.Equals(""))
            {
                SimulationPlaybook playbook = new SimulationPlaybook(config_params.playbook_sleep);
                playbook.tasks = new List<PlaybookTask>();
                if (techniques.Contains(","))
                {
                    string[] techs = techniques.Split(',');
                    for (int i = 0; i < techs.Length; i++)
                    {
                        PlaybookTask task = new PlaybookTask(techs[i], config_params.variation, config_params.task_sleep, config_params.cleanup);
                        playbook.tasks.Add(task);
                    }     
                }
                else
                {
                    PlaybookTask task = new PlaybookTask(techniques, config_params.variation, config_params.task_sleep, config_params.cleanup);
                    playbook.tasks.Add(task);
                }

                ExecutePlaybook(playbook, log);
                
                //ExecutePlaybookOld(techniques, variation, nusers, nhosts, pbsleep, tsleep, log, cleanup);
            }

        }
        public static void Scout(string rhost, string domain, string ruser, string rpwd, string scoutfpath, string log, string scout_action, string scout_np, bool verbose)
        {
            List<String> actions = new List<string>() { "all", "wef", "pws", "ps", "svcs", "auditpol", "cmdline" };

            if (!actions.Contains(scout_action))
            {
                Console.WriteLine("[*] Not supported.");
                Console.WriteLine("[*] Exiting");
                return;
            }
            if (rpwd == "")
            {
                Console.Write("Password for {0}\\{1}: ", domain, ruser);
                rpwd = Utils.GetPassword();
                Console.WriteLine();
            }
            string uploadPath = System.Reflection.Assembly.GetEntryAssembly().Location;
            int index = scoutfpath.LastIndexOf(@"\");
            string scoutFolder = scoutfpath.Substring(0, index + 1);
            string args = "/o";

            Console.WriteLine("[+] Uploading Scout to {0} on {1}", scoutfpath, rhost);
            RemoteLauncher.upload(uploadPath, scoutfpath, rhost, ruser, rpwd, domain);

            Console.WriteLine("[+] Executing the Scout via WMI ...");
            RemoteLauncher.wmiexec(rhost, scoutfpath, args, domain, ruser, rpwd);
            Console.WriteLine("[+] Connecting to the Scout ...");

            string result = NamedPipes.RunClient(rhost, domain, ruser, rpwd, scout_np, "SYN");
            if (result.Equals("SYN/ACK"))
            {
                Console.WriteLine("[+] OK");
                string results;

                if (scout_action.Equals("all"))
                {
                    string temp;

                    temp = NamedPipes.RunClient(rhost, domain, ruser, rpwd, scout_np, "wef");
                    results = Encoding.UTF8.GetString(Convert.FromBase64String(temp));

                    temp = NamedPipes.RunClient(rhost, domain, ruser, rpwd, scout_np, "pws");
                    results += Encoding.UTF8.GetString(Convert.FromBase64String(temp));

                    temp = NamedPipes.RunClient(rhost, domain, ruser, rpwd, scout_np, "cmdline");
                    results += Encoding.UTF8.GetString(Convert.FromBase64String(temp));

                    temp = NamedPipes.RunClient(rhost, domain, ruser, rpwd, scout_np, "ps");
                    results += Encoding.UTF8.GetString(Convert.FromBase64String(temp));

                    temp = NamedPipes.RunClient(rhost, domain, ruser, rpwd, scout_np, "svcs");
                    results += Encoding.UTF8.GetString(Convert.FromBase64String(temp));

                    temp = NamedPipes.RunClient(rhost, domain, ruser, rpwd, scout_np, "auditpol");
                    results += Encoding.UTF8.GetString(Convert.FromBase64String(temp));

                    NamedPipes.RunClient(rhost, domain, ruser, rpwd, scout_np, "quit");

                }
                else 
                {
                    results = NamedPipes.RunClient(rhost, domain, ruser, rpwd, scout_np, scout_action);
                    results = Encoding.UTF8.GetString(Convert.FromBase64String(results));
                    NamedPipes.RunClient(rhost, domain, ruser, rpwd, scout_np, "quit");
                }
                if (verbose)
                {
                    Console.WriteLine("[+] Grabbing the Scout output...");
                    System.Threading.Thread.Sleep(1000);
                    string sresults = RemoteLauncher.readFile(rhost, scoutFolder + log, ruser, rpwd, domain);
                    Console.WriteLine("[+] Results:");
                    Console.WriteLine();
                    Console.WriteLine(sresults);
                }
                Console.WriteLine("[+] Scout Results...");
                Console.WriteLine();
                Console.WriteLine(results);
                Console.WriteLine();
                Console.WriteLine("[+] Cleaning up...");
                Console.WriteLine("[+] Deleting " + @"\\" + rhost + @"\" + scoutfpath.Replace(":", "$"));
                RemoteLauncher.delete(scoutfpath, rhost, ruser, rpwd, domain);
                Console.WriteLine("[+] Deleting " + @"\\" + rhost + @"\" + (scoutFolder + log).Replace(":", "$"));
                RemoteLauncher.delete(scoutFolder + log, rhost, ruser, rpwd, domain);
            }
        }
        public static void ExecuteRemoteTechniquesSerialized(CommandlineParameters cmd_params)
        {
            // techniques that need to be executed from a high integrity process
            string[] privileged_techniques = new string[] { "T1003.001", "T1136.001", "T1070.001", "T1543.003", "T1546.003" };

            if (cmd_params.remote_password == "")
            {
                Console.Write("Password for {0}\\{1}: ", cmd_params.domain, cmd_params.remote_user);
                cmd_params.remote_password = Utils.GetPassword();
                Console.WriteLine();
            }

            string uploadPath = System.Reflection.Assembly.GetEntryAssembly().Location;
            int index = cmd_params.scout_full_path.LastIndexOf(@"\");
            string scoutFolder = cmd_params.scout_full_path.Substring(0, index + 1);

            System.Threading.Thread.Sleep(3000);

            if (cmd_params.opsec)
            {
                string result = "";
                string args = "/o";

                Console.WriteLine("[+] Uploading and executing the Scout on {0} ", @"\\" + cmd_params.remote_host + @"\" + cmd_params.scout_full_path.Replace(":", "$"));
                RemoteLauncher.upload(uploadPath, cmd_params.scout_full_path, cmd_params.remote_host, cmd_params.remote_user, cmd_params.remote_password, cmd_params.domain);
                RemoteLauncher.wmiexec(cmd_params.remote_host, cmd_params.scout_full_path, args, cmd_params.domain, cmd_params.remote_user, cmd_params.remote_password);
                Console.WriteLine("[+] Connecting to the Scout ...");



                SimulationPlaybook playbook = new SimulationPlaybook();
                List<PlaybookTask> tasks = new List<PlaybookTask>();

                if (cmd_params.techniques.Contains(","))
                {
                    string[] techs = cmd_params.techniques.Split(',');
                    for (int i = 0; i < techs.Length; i++)
                    {
                        tasks.Add(new PlaybookTask(techs[i], cmd_params.variation, cmd_params.task_sleep, cmd_params.cleanup));

                    }
                }
                else
                {
                    tasks.Add(new PlaybookTask(cmd_params.techniques, cmd_params.variation, cmd_params.task_sleep, cmd_params.cleanup));

                }
                playbook.tasks = tasks;
                playbook.simulator_relative_path = cmd_params.simulator_relative_path;
                playbook.playbook_sleep = cmd_params.playbook_sleep;

                SimulationRequest sim_request = new SimulationRequest("SYN", "regular", playbook);
                if (privileged_techniques.Contains(cmd_params.techniques.ToUpper())) sim_request.recon_type = "privileged";
                byte[] bytes_sim_rqeuest = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sim_request));
                result = NamedPipes.RunClientSerialized(cmd_params.remote_host, cmd_params.domain, cmd_params.remote_user, cmd_params.remote_password, cmd_params.scout_namepipe, bytes_sim_rqeuest);
                SimulationResponse sim_response = JsonConvert.DeserializeObject<SimulationResponse>(result);

                if (sim_response.header.Equals("SYN/ACK"))
                {
                    Console.WriteLine("[+] OK");
                    string duser = sim_response.recon_response.user;
                    if (duser == "")
                    {
                        Console.WriteLine("[!] Could not identify a suitable process for the simulation. Is a user logged in on: " + cmd_params.remote_host + "?");
                        sim_request = new SimulationRequest("FIN");
                        result = NamedPipes.RunClientSerialized(cmd_params.remote_host, cmd_params.domain, cmd_params.remote_user, cmd_params.remote_password, cmd_params.scout_full_path, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sim_request)));
                        Thread.Sleep(1000);
                        RemoteLauncher.delete(cmd_params.scout_full_path, cmd_params.remote_host, cmd_params.remote_user, cmd_params.remote_password, cmd_params.domain);
                        RemoteLauncher.delete(scoutFolder + cmd_params.log_filename, cmd_params.remote_host, cmd_params.remote_user, cmd_params.remote_password, cmd_params.domain);
                        Console.WriteLine("[!] Exitting.");
                        return;
                    }
                    else
                    {
                        string user = duser.Split('\\')[1];
                        Console.WriteLine("[!] Recon -> " + String.Format("Identified logged user: {0}", duser));
                        string simfpath = "C:\\Users\\" + user + "\\" + cmd_params.simulator_relative_path;
                        int index2 = cmd_params.simulator_relative_path.LastIndexOf(@"\");
                        string simrfolder = cmd_params.simulator_relative_path.Substring(0, index2 + 1);

                        string simfolder = "C:\\Users\\" + user + "\\" + simrfolder;

                        Console.WriteLine("[+] Uploading Simulator to " + @"\\" + cmd_params.remote_host + @"\" + simfpath.Replace(":", "$"));
                        RemoteLauncher.upload(uploadPath, simfpath, cmd_params.remote_host, cmd_params.remote_user, cmd_params.remote_password, cmd_params.domain);

                        Console.WriteLine("[+] Triggering simulation using PPID Spoofing | Process: {0}.exe | PID: {1} | High Integrity: {2}", sim_response.recon_response.process, sim_response.recon_response.process_id, sim_response.recon_response.process_integrity);
                        sim_request = new SimulationRequest("ACT");
                        result = NamedPipes.RunClientSerialized(cmd_params.remote_host, cmd_params.domain, cmd_params.remote_user, cmd_params.remote_password, cmd_params.scout_namepipe, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sim_request)));

                        if (cmd_params.verbose)
                        {
                            Console.WriteLine("[+] Grabbing the Scout output...");
                            System.Threading.Thread.Sleep(1000);
                            string sresults = RemoteLauncher.readFile(cmd_params.remote_host, scoutFolder + cmd_params.log_filename, cmd_params.remote_user, cmd_params.remote_password, cmd_params.domain);
                            Console.WriteLine("[+] Results:");
                            Console.WriteLine();
                            Console.WriteLine(sresults);
                        }
                        Thread.Sleep(5000);
                        bool finished = false;
                        int counter = 1;
                        string results = RemoteLauncher.readFile(cmd_params.remote_host, simfolder + cmd_params.log_filename, cmd_params.remote_user, cmd_params.remote_password, cmd_params.domain);
                        while (finished == false)
                        {

                            if (results.Split('\n').Last().Contains("Playbook Finished"))
                            {
                                //Console.WriteLine("[+] Obtaining the Simulator output...");
                                Console.WriteLine("[+] Results:");
                                Console.WriteLine();
                                Console.WriteLine(results);
                                Console.WriteLine();
                                Console.WriteLine("[+] Cleaning up...");
                                Console.WriteLine("[+] Deleting " + @"\\" + cmd_params.remote_host + @"\" + cmd_params.scout_full_path.Replace(":", "$"));
                                RemoteLauncher.delete(cmd_params.scout_full_path, cmd_params.remote_host, cmd_params.remote_user, cmd_params.remote_password, cmd_params.domain);
                                Console.WriteLine("[+] Deleting " + @"\\" + cmd_params.remote_host + @"\" + (scoutFolder + cmd_params.log_filename).Replace(":", "$"));
                                RemoteLauncher.delete(scoutFolder + cmd_params.log_filename, cmd_params.remote_host, cmd_params.remote_user, cmd_params.remote_password, cmd_params.domain);
                                Console.WriteLine("[+] Deleting " + @"\\" + cmd_params.remote_host + @"\" + simfpath.Replace(":", "$"));
                                RemoteLauncher.delete(simfpath, cmd_params.remote_host, cmd_params.remote_user, cmd_params.remote_password, cmd_params.domain);
                                Console.WriteLine("[+] Deleting " + @"\\" + cmd_params.remote_host + @"\" + (simfolder + cmd_params.log_filename).Replace(":", "$"));
                                RemoteLauncher.delete(simfolder + cmd_params.log_filename, cmd_params.remote_host, cmd_params.remote_user, cmd_params.remote_password, cmd_params.domain);
                                finished = true;
                            }
                            else
                            {
                                Console.WriteLine("[+] Not finished. Waiting an extra {0} seconds", counter * 10);
                                Thread.Sleep(counter * 10 * 1000);
                                results = RemoteLauncher.readFile(cmd_params.remote_host, simfolder + cmd_params.log_filename, cmd_params.remote_user, cmd_params.remote_password, cmd_params.domain);
                            }
                            counter += 1;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("[!] Could not connect to namedpipe service");
                    Console.WriteLine("[!] Exitting.");
                    return;
                }
            }
            else
            {
                Console.WriteLine("[+] Uploading and executing the Simulator on {0} ", @"\\" + cmd_params.remote_host + @"\" + cmd_params.scout_full_path.Replace(":", "$"));
                RemoteLauncher.upload(uploadPath, cmd_params.scout_full_path, cmd_params.remote_host, cmd_params.remote_user, cmd_params.remote_password, cmd_params.domain);
                RemoteLauncher.wmiexec(cmd_params.remote_host, cmd_params.scout_full_path, "/s", cmd_params.domain, cmd_params.remote_user, cmd_params.remote_password);
                Thread.Sleep(2000);
                if (cmd_params.cleanup) NamedPipes.RunClient(cmd_params.remote_host, cmd_params.domain, cmd_params.remote_user, cmd_params.remote_password, cmd_params.simulator_namedpipe, "technique:" + cmd_params.techniques + " variation:" + cmd_params.variation.ToString() + " pbsleep:" + cmd_params.playbook_sleep.ToString() + " tsleep:" + cmd_params.task_sleep.ToString() + " cleanup:True");
                else NamedPipes.RunClient(cmd_params.remote_host, cmd_params.domain, cmd_params.remote_user, cmd_params.remote_password, cmd_params.simulator_namedpipe, "technique:" + cmd_params.techniques + " variation:" + cmd_params.variation.ToString() + " pbsleep:" + cmd_params.playbook_sleep.ToString() + " tsleep:" + cmd_params.task_sleep.ToString() + " cleanup:False");

                Thread.Sleep(5000);
                bool finished = false;
                int counter = 1;
                string results = RemoteLauncher.readFile(cmd_params.remote_host, scoutFolder + cmd_params.log_filename, cmd_params.remote_user, cmd_params.remote_password, cmd_params.domain);
                while (finished == false)
                {

                    if (results.Split('\n').Last().Contains("Playbook Finished"))
                    {
                        Console.WriteLine("[+] Obtaining results...");
                        Console.WriteLine("[+] Results:");
                        Console.WriteLine();
                        Console.WriteLine(results);
                        Console.WriteLine();
                        Console.WriteLine("[+] Cleaning up...");
                        Console.WriteLine("[+] Deleting " + @"\\" + cmd_params.remote_host + @"\" + cmd_params.scout_full_path.Replace(":", "$"));
                        RemoteLauncher.delete(cmd_params.scout_full_path, cmd_params.remote_host, cmd_params.remote_user, cmd_params.remote_password, cmd_params.domain);
                        Console.WriteLine("[+] Deleting " + @"\\" + cmd_params.remote_host + @"\" + (scoutFolder + cmd_params.log_filename).Replace(":", "$"));
                        RemoteLauncher.delete(scoutFolder + cmd_params.log_filename, cmd_params.remote_host, cmd_params.remote_user, cmd_params.remote_password, cmd_params.domain);
                        finished = true;
                    }
                    else
                    {
                        Console.WriteLine("[+] Not finished. Waiting an extra {0} seconds", counter * 10);
                        Thread.Sleep(counter * 10 * 1000);
                        results = RemoteLauncher.readFile(cmd_params.remote_host, scoutFolder + cmd_params.log_filename, cmd_params.remote_user, cmd_params.remote_password, cmd_params.domain);
                    }
                    counter += 1;
                }
            }
        }
        public static SimulationPlaybookResult ExecuteRemoteTechniquesJsonSerialized(string rhost, string domain, string ruser, string rpwd, string scout_np, SimulationPlaybook playbook, string log, bool verbose)
        {
            // techniques that need to be executed from a high integrity process
            string[] privileged_techniques = new string[] { "T1003.001", "T1136.001", "T1070.001", "T1543.003", "T1546.003" };

            string uploadPath = System.Reflection.Assembly.GetEntryAssembly().Location;
            int index = playbook.scout_full_path.LastIndexOf(@"\");
            string scoutFolder = playbook.scout_full_path.Substring(0, index + 1);
            Thread.Sleep(3000);

            if (playbook.opsec.Equals("ppid"))
            {
                string result = "";
                string args = "/o";

                //Console.WriteLine("[+] Uploading Scout to {0} on {1}", scoutfpath, rhost);
                RemoteLauncher.upload(uploadPath, playbook.scout_full_path, rhost, ruser, rpwd, domain);

                //Console.WriteLine("[+] Executing the Scout via WMI ...");
                RemoteLauncher.wmiexec(rhost, playbook.scout_full_path, args, domain, ruser, rpwd);
                //Console.WriteLine("[+] Connecting to namedpipe service ...");

                SimulationRequest sim_request = new SimulationRequest("SYN", "regular", playbook);
                //if (privileged_techniques.Contains(techniques.ToUpper())) sim_request.recon_type = "privileged";
                byte[] bytes_sim_rqeuest = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sim_request));
                result = NamedPipes.RunClientSerialized(rhost, domain, ruser, rpwd, scout_np, bytes_sim_rqeuest);
                SimulationResponse sim_response = JsonConvert.DeserializeObject<SimulationResponse>(result);



                if (sim_response.header.Equals("SYN/ACK"))
                {
                    //Console.WriteLine("[+] OK");
                    string duser = sim_response.recon_response.user;
                    if (duser == "")
                    {

                        Console.WriteLine("[!] Could not identify a suitable process for the simulation. Is a user logged in on: " + rhost + "?");
                        sim_request = new SimulationRequest("FIN");
                        result = NamedPipes.RunClientSerialized(rhost, domain, ruser, rpwd, scout_np, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sim_request)));
                        Thread.Sleep(1000);
                        RemoteLauncher.delete(playbook.scout_full_path, rhost, ruser, rpwd, domain);
                        RemoteLauncher.delete(scoutFolder + log, rhost, ruser, rpwd, domain);
                        //Console.WriteLine("[!] Exitting.");
                        return null;
                    }
                    else
                    {
                        string user = duser.Split('\\')[1];
                        string simfpath = "C:\\Users\\" + user + "\\" + playbook.simulator_relative_path;
                        int index2 = playbook.simulator_relative_path.LastIndexOf(@"\");
                        string simrfolder = playbook.simulator_relative_path.Substring(0, index2 + 1);

                        string simfolder = "C:\\Users\\" + user + "\\" + simrfolder;

                        //Console.WriteLine("[+] Uploading Simulator to " + simfpath);
                        RemoteLauncher.upload(uploadPath, simfpath, rhost, ruser, rpwd, domain);

                        //Console.WriteLine("[+] Triggering simulation using PPID Spoofing | Process: {0}.exe | PID: {1} | High Integrity: {2}", sim_response.recon_response.process, sim_response.recon_response.process_id, sim_response.recon_response.process_integrity);
                        sim_request = new SimulationRequest("ACT");
                        result = NamedPipes.RunClientSerialized(rhost, domain, ruser, rpwd, scout_np, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sim_request)));

                        System.Threading.Thread.Sleep(5000);
                        bool finished = false;
                        int counter = 1;
                        string results = RemoteLauncher.readFile(rhost, simfolder + log, ruser, rpwd, domain);
                        while (finished == false)
                        {
                            if (results.Split('\n').Last().Contains("Playbook Finished"))
                            {
                                Console.WriteLine("[+] Results:");
                                Console.WriteLine();
                                Console.WriteLine(results);
                                Console.WriteLine();
                                RemoteLauncher.delete(playbook.scout_full_path, rhost, ruser, rpwd, domain);
                                RemoteLauncher.delete(scoutFolder + log, rhost, ruser, rpwd, domain);
                                RemoteLauncher.delete(simfpath, rhost, ruser, rpwd, domain);
                                RemoteLauncher.delete(simfolder + log, rhost, ruser, rpwd, domain);
                                finished = true;
                            }
                            else
                            {
                                Console.WriteLine("[+] Not finished. Waiting an extra {0} seconds", counter * 10);
                                Thread.Sleep(counter * 10 * 1000);
                                results = RemoteLauncher.readFile(rhost, simfolder + log, ruser, rpwd, domain);
                            }
                            counter += 1;
                        }
                        return Json.GetPlaybookResult(results);

                    }
                }
                else
                {
                    //Console.WriteLine("[!] Could not connect to namedpipe service");
                    return null;
                }
            }
            else
            {
                //Console.WriteLine("[+] Uploading PurpleSharp to {0} on {1}", scoutfpath, rhost);
                RemoteLauncher.upload(uploadPath, playbook.scout_full_path, rhost, ruser, rpwd, domain);

                string cmdline = "/t ";// + techniques;
                //Console.WriteLine("[+] Executing PurpleSharp via WMI ...");
                RemoteLauncher.wmiexec(rhost, playbook.scout_full_path, cmdline, domain, ruser, rpwd);
                Thread.Sleep(3000);
                Console.WriteLine("[+] Obtaining results...");
                string results = RemoteLauncher.readFile(rhost, scoutFolder + log, ruser, rpwd, domain);
                Console.WriteLine("[+] Results:");
                Console.WriteLine();
                Console.WriteLine(results);
                //Console.WriteLine("[+] Cleaning up...");
                //Console.WriteLine("[+] Deleting " + @"\\" + rhost + @"\" + scoutfpath.Replace(":", "$"));
                RemoteLauncher.delete(playbook.scout_full_path, rhost, ruser, rpwd, domain);
                //
                //Console.WriteLine("[+] Deleting " + @"\\" + rhost + @"\" + (scoutFolder + log).Replace(":", "$"));
                RemoteLauncher.delete(scoutFolder + log, rhost, ruser, rpwd, domain);

                return Json.GetPlaybookResult(results);
            }
        }
        public static void ExecutePlaybookTechnique(PlaybookTask task, string log)
        {
            var rand = new Random();

            switch (task.technique)
            {
                //// Initial Access ////

                //// Execution ////

                case "T1059.001":
                    if (task.variation == 1) Simulations.Execution.ExecutePowershellCmd(log);
                    else Simulations.Execution.ExecutePowershellNET(log);
                    break;

                case "T1059.003":
                    Simulations.Execution.WindowsCommandShell(log);
                    break;


                case "T1059.005":
                    Simulations.Execution.VisualBasic(log);
                    break;

                case "T1059.007":
                    Simulations.Execution.JScript(log);
                    break;

                case "T1569.002":
                    Simulations.Execution.ServiceExecution(log);
                    break;


                //T1021.006 - Windows Remote Management

                //// Persistence ////

                //T1053.005 - Scheduled Task

                case "T1053.005":
                    Simulations.Persistence.CreateScheduledTaskCmd(log, task.cleanup);
                    break;

                case "T1136.001":
                    if (task.variation == 1) Simulations.Persistence.CreateLocalAccountApi(log, task.cleanup);
                    else Simulations.Persistence.CreateLocalAccountCmd(log, task.cleanup);
                    break;

                case "T1543.003":
                    if (task.variation == 1) Simulations.Persistence.CreateWindowsServiceApi(log, task.cleanup);
                    else Simulations.Persistence.CreateWindowsServiceCmd(log, task.cleanup);
                    break;

                case "T1547.001":
                    if (task.variation == 1) Simulations.Persistence.CreateRegistryRunKeyNET(log, task.cleanup);
                    else Simulations.Persistence.CreateRegistryRunKeyCmd(log, task.cleanup);
                    break;

                case "T1546.003":
                    Simulations.Persistence.WMIEventSubscription(log, task.cleanup);
                    break;

                //// Privilege Escalation  ////

                //T1543.003 - New Service

                //T1053.005 - Scheduled Task

                //// Defense Evasion ////

                case "T1218.010":
                    Simulations.DefenseEvasion.Regsvr32(log);
                    break;

                case "T1218.009":
                    Simulations.DefenseEvasion.RegsvcsRegasm(log);
                    break;

                case "T1218.004":
                    Simulations.DefenseEvasion.InstallUtil(log);
                    break;

                case "T1140":
                    Simulations.DefenseEvasion.DeobfuscateDecode(log);
                    break;

                case "T1218.005":
                    Simulations.DefenseEvasion.Mshta(log);
                    break;

                case "T1218.003":
                    Simulations.DefenseEvasion.Csmtp(log);
                    break;

                case "T1197":
                    Simulations.DefenseEvasion.BitsJobs(log);
                    break;

                case "T1218.011":
                    Simulations.DefenseEvasion.Rundll32(log);
                    break;

                case "T1070.001":
                    if (task.variation == 1) Simulations.DefenseEvasion.ClearSecurityEventLogNET(log);
                    else Simulations.DefenseEvasion.ClearSecurityEventLogCmd(log);

                    break;

                case "T1220":
                    Simulations.DefenseEvasion.XlScriptProcessing(log);
                    break;

                case "T1055.002":
                    Simulations.DefenseEvasion.PortableExecutableInjection(log);
                    break;

                case "T1055.003":
                    Simulations.DefenseEvasion.ThreadHijack(log);
                    break;

                case "T1055.004":
                    Simulations.DefenseEvasion.AsynchronousProcedureCall(log);
                    break;

                case "T1134.004":
                    Simulations.DefenseEvasion.ParentPidSpoofing(log);
                    break;



                //T1218.010 - Regsvr32


                ////  Credential Access //// 

                //T1110.003 - Brute Force
                case "T1110.003":
                    string password = "Summer2020";
                    if (task.variation == 1) Simulations.CredAccess.LocalDomainPasswordSpray(task.target_users, task.task_sleep, password, log);
                    else Simulations.CredAccess.RemotePasswordSpray(task.target_hosts, task.target_users, task.task_sleep, password, log);

                    break;

                //T1558.003 - Kerberoasting
                case "T1558.003":
                    Simulations.CredAccess.Kerberoasting(log, task.task_sleep);
                    break;

                //T1003.001 - LSASS Memory
                case "T1003.001":
                    Simulations.CredAccess.LsassMemoryDump(log);
                    break;

                ////  Discovery //// 

                //T1016 System Network Configuration Discovery
                case "T1016":
                    Simulations.Discovery.SystemNetworkConfigurationDiscovery(log);
                    break;

                //T1083 File and Directory Discovery
                case "T1083":
                    Simulations.Discovery.FileAndDirectoryDiscovery(log);
                    break;

                //T1135 - Network Share Discovery
                case "T1135":
                    Simulations.Discovery.EnumerateShares(task.target_hosts, task.task_sleep, log);
                    break;

                //T1046 - Network Service Scanning
                case "T1046":
                    Simulations.Discovery.NetworkServiceDiscovery(task.target_hosts, task.task_sleep, log);
                    break;

                case "T1087.001":
                    Simulations.Discovery.LocalAccountDiscoveryCmd(log);
                    break;

                case "T1087.002":
                    if (task.variation == 1) Simulations.Discovery.DomainAccountDiscoveryLdap(log);
                    else Simulations.Discovery.DomainAccountDiscoveryCmd(log);
                    break;

                case "T1007":
                    Simulations.Discovery.SystemServiceDiscovery(log);
                    break;

                case "T1033":
                    Simulations.Discovery.SystemUserDiscovery(log);
                    break;

                case "T1049":
                    Simulations.Discovery.SystemNetworkConnectionsDiscovery(log);
                    break;

                case "T1482":
                    Simulations.Discovery.DomainTrustDiscovery(log);
                    break;

                case "T1201":
                    Simulations.Discovery.PasswordPolicyDiscovery(log);
                    break;

                case "T1069.001":
                    Simulations.Discovery.LocalGroups(log);
                    break;

                case "T1069.002":
                    Simulations.Discovery.DomainGroups(log);
                    break;

                case "T1012":
                    Simulations.Discovery.QueryRegistry(log);
                    break;

                case "T1518.001":
                    Simulations.Discovery.SecuritySoftwareDiscovery(log);
                    break;

                case "T1082":
                    Simulations.Discovery.SystemInformationDiscovery(log);
                    break;

                case "T1124":
                    Simulations.Discovery.SystemTimeDiscovery(log);
                    break;

                ////  Lateral Movement //// 

                //T1021.006 - Windows Remote Management
                case "T1021.006":
                    Simulations.LateralMovement.WinRmCodeExec(task.target_hosts, task.task_sleep, log);
                    break;

                //T1021 - Remote Service
                case "T1021":
                    Simulations.LateralMovement.CreateRemoteServiceOnHosts(task.target_hosts, task.task_sleep, task.cleanup, log);
                    break;

                //T1047 - Windows Management Instrumentation
                case "T1047":
                    Simulations.LateralMovement.ExecuteWmiOnHosts(task.target_hosts, task.task_sleep, log);
                    break;

                // Collection

                // Command and Control

                // Exfiltration

                // Impact

                // Other Techniques

                case "privenum":
                    Simulations.Discovery.PrivilegeEnumeration(task.target_hosts, task.task_sleep, log);
                    break;

                default:
                    break;

            }
        }
        public static void ExecutePlaybook(SimulationPlaybook playbook, string log)
        {
            string currentPath = AppDomain.CurrentDomain.BaseDirectory;
            Logger logger = new Logger(currentPath + log);
            PlaybookTask lastTask = playbook.tasks.Last();
            foreach (PlaybookTask task in playbook.tasks)
            {
                ExecutePlaybookTechnique(task, log);
                if (playbook.playbook_sleep > 0 && task != lastTask ) Thread.Sleep(1000 * playbook.playbook_sleep);
                logger.TimestampInfo("Playbook Finished");
            }
        }

    }
}
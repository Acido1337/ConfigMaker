﻿using ConfigMaker.Csgo.Commands;
using ConfigMaker.Csgo.Config;
using ConfigMaker.Csgo.Config.Entries;
using ConfigMaker.Csgo.Config.Entries.interfaces;
using ConfigMaker.Csgo.Config.Enums;
using ConfigMaker.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;
using Res = ConfigMaker.Properties.Resources;

namespace ConfigMaker.Mvvm.Models
{
    public class MainModel: BindableBase
    {
        public enum EntryStateBinding
        {
            KeyDown,
            KeyUp,
            Default,
            Alias,
            InvalidState
        }

        public ObservableCollection<BindableBase> ActionTabItems { get; } = 
            new ObservableCollection<BindableBase>();
        public BuyMenuModel BuyMenuModel { get; private set; }
        public ObservableCollection<SettingsCategoryModel> SettingsCategoryModels { get; } =
            new ObservableCollection<SettingsCategoryModel>();

        public EntryStateBinding StateBinding
        {
            get => _stateBinding;
            set
            {
                if (SetProperty(ref _stateBinding, (CoerceStateBinding(value))))
                {
                    foreach (EntryController controller in this.entryControllers)
                        controller.HandleState(this.StateBinding);
                }
            }   
        }

        public string CustomCfgPath
        {
            get => _customCfgPath;
            set => SetProperty(ref _customCfgPath, value);
        }

        public string CustomCfgName
        {
            get => _customCfgName;
            set => SetProperty(ref _customCfgName, value.Trim());
        }


        public MainModel()
        {
            this.KeyDownAttachments = new AttachmentsModel();
            this.KeyUpAttachments = new AttachmentsModel();
            this.SolidAttachments = new AttachmentsModel()
            {
                Hint = Localize(Res.CommandsByDefault_Hint)
            };

            ReadConfig();

            InitActionTab();
            InitBuyTab();
            InitGameSettingsTab();
        }

        ConfigManager cfgManager = new ConfigManager();
        KeySequence currentKeySequence = null;
        AppConfig cfg = null;
        string cfgPath = $"{nameof(AppConfig)}.xml";
        string _customCfgName = string.Empty;
        string _customCfgPath = string.Empty;
        ObservableCollection<EntryController> entryControllers = new ObservableCollection<EntryController>();
        EntryStateBinding _stateBinding;
        
        //AliasControllerViewModel aliasSetVM = null;
        public AttachmentsModel KeyDownAttachments { get; }
        public AttachmentsModel KeyUpAttachments { get; }
        public AttachmentsModel SolidAttachments { get; }

        void InitActionTab()
        {
            //// Локальный метод для подготовки и настройки нового чекбокса-контроллера
            ActionModel RegisterAction(string cmd, bool isMeta)
            {
                ActionModel actionModel = new ActionModel()
                {
                    Content = Localize(cmd),
                    Key = cmd,
                    ToolTip = isMeta ? $"+{cmd.ToLower()}" : $"{cmd.ToLower()}"
                };

                this.ActionTabItems.Add(actionModel);
                return actionModel;
            }

            // Локальный метод для добавления действий
            void AddAction(string cmd, bool isMeta)
            {
                ActionModel actionModel = RegisterAction(cmd, isMeta);

                EntryController entryController = new EntryController
                {
                    Model = actionModel,
                    // генерируем каждый раз новый элемент во избежание замыкания
                    Generate = () =>
                    {
                        return new Entry()
                        {
                            PrimaryKey = cmd,
                            Cmd = new SingleCmd(cmd),
                            IsMetaScript = isMeta,
                            Type = EntryType.Static
                        };
                    },
                    UpdateUI = (entry) => actionModel.IsChecked = true,
                    Focus = () =>
                    {
                        //actionTabButton.IsChecked = true;
                        actionModel.IsFocused = true;
                    },
                    Restore = () => actionModel.IsChecked = false,
                    HandleState = (state) =>
                    {
                        actionModel.IsEnabled =
                            state != EntryStateBinding.InvalidState
                            && (!isMeta || state == EntryStateBinding.KeyDown);
                    }
                };
                entryControllers.Add(entryController);
            };

            // Метод для добавления новой категории.
            void AddActionGroupHeader(string text)
            {
                TextModel headerVM = new TextModel()
                {
                    Text = text
                };

                this.ActionTabItems.Add(headerVM);
            };

            AddActionGroupHeader(Res.CategoryCommonActions);
            AddAction("attack", true);
            AddAction("attack2", true);
            AddAction("reload", true);
            AddAction("drop", false);
            AddAction("use", true);
            AddAction("showscores", true);

            AddActionGroupHeader(Res.CategoryMovement);
            AddAction("forward", true);
            AddAction("back", true);
            AddAction("moveleft", true);
            AddAction("moveright", true);
            AddAction("jump", true);
            AddAction("duck", true);
            AddAction("speed", true);

            AddActionGroupHeader(Res.CategoryEquipment);
            AddAction("slot1", false);
            AddAction("slot2", false);
            AddAction("slot3", false);
            AddAction("slot4", false);
            AddAction("slot5", false);
            AddAction("slot6", false);
            AddAction("slot7", false);
            AddAction("slot8", false);
            AddAction("slot9", false);
            AddAction("slot10", false);
            AddAction("slot11", false);
            AddAction("invprev", false);
            AddAction("invnext", false);
            AddAction("lastinv", false);
            AddAction("cl_show_team_equipment", true);

            AddActionGroupHeader(Res.CategoryBuy);
            AddAction("buymenu", false);
            AddAction("autobuy", false);
            AddAction("rebuy", false);

            AddActionGroupHeader(Res.CategoryCommunication);
            AddAction("voicerecord", true);
            AddAction("radio1", false);
            AddAction("radio2", false);
            AddAction("radio3", false);
            AddAction("messagemode2", false);
            AddAction("messagemode", false);

            AddActionGroupHeader(Res.CategoryWarmup);
            AddAction("god", false);
            AddAction("noclip", false);
            AddAction("impulse 101", false);
            AddAction("mp_warmup_start", false);
            AddAction("mp_warmup_end", false);
            AddAction("mp_swapteams", false);
            AddAction("bot_add_t", false);
            AddAction("bot_add_ct", false);
            AddAction("bot_place", false);

            AddActionGroupHeader(Res.CategoryOther);
            AddAction("lookatweapon", true);
            AddAction("spray_menu", true);
            AddAction("r_cleardecals", false);
            AddAction("callvote", false);
            AddAction("teammenu", false);
            AddAction("toggleconsole", false);
            AddAction("clear", false);
            AddAction("cl_clearhinthistory", false);
            AddAction("screenshot", false);
            AddAction("pause", false);
            AddAction("disconnect", false);
            AddAction("quit", false);

            AddActionGroupHeader(Res.CategoryDemo);
            AddAction("demo_resume", false);
            AddAction("demo_togglepause", false);

            AddActionGroupHeader(Res.CategoryBonusScripts);

            // jumpthrow script
            const string jumpthrowEntryKey = "Jumpthrow";
            ActionModel jumpthrowVM = RegisterAction(jumpthrowEntryKey, true);

            this.entryControllers.Add(new EntryController()
            {
                Model = jumpthrowVM,
                Focus = () =>
                {
                    //actionTabButton.IsChecked = true;
                    jumpthrowVM.IsFocused = true;
                },
                Generate = () =>
                {
                    MetaCmd metaCmd = new MetaCmd(
                        jumpthrowEntryKey,
                        Executable.SplitCommands("+jump; -attack; -attack2"),
                        Executable.SplitCommands("-jump"));

                    return new Entry()
                    {
                        PrimaryKey = jumpthrowEntryKey,
                        Cmd = new SingleCmd(jumpthrowEntryKey),
                        IsMetaScript = true,
                        Type = EntryType.Static,
                        Dependencies = metaCmd
                    };
                },
                UpdateUI = (entry) => jumpthrowVM.IsChecked = true,
                Restore = () => jumpthrowVM.IsChecked = false,
                HandleState = (state) => jumpthrowVM.IsEnabled = state == EntryStateBinding.KeyDown
            });

            // DisplayDamageOn
            const string displayDamageOnEntryKey = "DisplayDamage_On";
            ActionModel displayDamageOnVM = RegisterAction(displayDamageOnEntryKey, false);

            this.entryControllers.Add(new EntryController()
            {
                Model = displayDamageOnVM,
                Focus = () =>
                {
                    //actionTabButton.IsChecked = true;
                    displayDamageOnVM.IsFocused = true;
                },
                Generate = () =>
                {
                    string[] stringCmds = new string[]
                    {
                            "developer 1",
                            "con_filter_text \"Damage Given\"",
                            "con_filter_text_out \"Player:\"",
                            "con_filter_enable 2"
                    };
                    CommandCollection cmds = new CommandCollection(
                        stringCmds.Select(cmd => new SingleCmd(cmd)));

                    return new Entry()
                    {
                        PrimaryKey = displayDamageOnEntryKey,
                        Cmd = new SingleCmd(displayDamageOnEntryKey),
                        IsMetaScript = false,
                        Type = EntryType.Static,
                        Dependencies = new CommandCollection(new AliasCmd(displayDamageOnEntryKey, cmds))
                    };
                },
                UpdateUI = (entry) => displayDamageOnVM.IsChecked = true,
                Restore = () => displayDamageOnVM.IsChecked = false,
                HandleState = (state) => displayDamageOnVM.IsEnabled = state != EntryStateBinding.InvalidState
            });

            // DisplayDamageOff
            const string displayDamageOffEntryKey = "DisplayDamage_Off";
            ActionModel displayDamageOffVM = RegisterAction(displayDamageOffEntryKey, false);

            this.entryControllers.Add(new EntryController()
            {
                Model = displayDamageOffVM,
                Focus = () =>
                {
                    //actionTabButton.IsChecked = true;
                    displayDamageOffVM.IsFocused = true;
                },
                Generate = () =>
                {
                    string[] stringCmds = new string[]
                    {
                            "con_filter_enable 0",
                            "developer 0",
                            "echo \"Display damage: Off!\""
                    };
                    CommandCollection cmds = new CommandCollection(
                        stringCmds.Select(cmd => new SingleCmd(cmd)));

                    return new Entry()
                    {
                        PrimaryKey = displayDamageOffEntryKey,
                        Cmd = new SingleCmd(displayDamageOffEntryKey),
                        IsMetaScript = false,
                        Type = EntryType.Static,
                        Dependencies = new CommandCollection(new AliasCmd(displayDamageOffEntryKey, cmds))
                    };
                },
                UpdateUI = (entry) => displayDamageOffVM.IsChecked = true,
                Restore = () => displayDamageOffVM.IsChecked = false,
                HandleState = (state) => displayDamageOffVM.IsEnabled = state != EntryStateBinding.InvalidState
            });
        }

        void InitBuyTab()
        {
            const string buyScenarioEntryKey = "BuyScenario";

            BuyMenuModel buyMenuModel = new BuyMenuModel()
            {
                Content = Localize(buyScenarioEntryKey),
                Key = buyScenarioEntryKey
            };
            this.BuyMenuModel = buyMenuModel;

            // Локальный метод для получения всего оружия
            List<EntryModel> GetWeapons()
            {
                return buyMenuModel.Categories.SelectMany(c => c.Weapons).ToList();
            };

            // Обработчик интерфейса настроек закупки
            this.entryControllers.Add(new EntryController()
            {
                Model = this.BuyMenuModel,
                //Focus = () => buyTabButton.IsChecked = true,
                UpdateUI = (entry) =>
                {
                    IParametrizedEntry<string[]> extendedEntry = (IParametrizedEntry<string[]>)entry;
                    string[] weapons = extendedEntry.Arg;

                    // зададим состояние чекбоксов согласно аргументам
                    GetWeapons().ForEach(weaponModel => weaponModel.IsChecked = weapons.Contains((weaponModel.Key)));
                    // не забываем про главный чекбокс
                    this.BuyMenuModel.IsChecked = true;
                },
                Generate = () =>
                {
                    string[] weaponsToBuy = GetWeapons()
                        .Where(weaponVM => weaponVM.IsChecked)
                        .Select(weaponVM => weaponVM.Key).ToArray();

                    Executable cmd = null;
                    CommandCollection dependencies = null;

                    List<SingleCmd> buyCmds = weaponsToBuy.Select(weapon => new SingleCmd($"buy {weapon}")).ToList();

                    if (weaponsToBuy.Length == 1)
                    {
                        cmd = buyCmds[0];
                        dependencies = new CommandCollection();
                    }
                    else
                    {
                        AliasCmd buyAlias = new AliasCmd($"{GeneratePrefix()}_{buyScenarioEntryKey}", buyCmds);

                        cmd = buyAlias.Name;
                        dependencies = new CommandCollection(buyAlias);
                    }

                    return new ParametrizedEntry<string[]>()
                    {
                        PrimaryKey = buyScenarioEntryKey,
                        Type = EntryType.Dynamic,
                        Dependencies = dependencies,
                        Cmd = cmd,
                        Arg = weaponsToBuy,
                        IsMetaScript = false
                    };
                },
                Restore = () =>
                {
                    this.BuyMenuModel.IsChecked = false;
                    GetWeapons().ForEach(c => c.IsChecked = false);
                },
                HandleState = (state) => this.BuyMenuModel.IsEnabled = state != EntryStateBinding.InvalidState
            });

            //StackPanel currentPanel = null;
            WeaponCategoryModel currentCategory = null;

            void AddWeapon(string weaponId, string localizedName)
            {
                EntryModel weaponModel = new EntryModel
                {
                    Content = localizedName,
                    Key = weaponId
                };

                weaponModel.PropertyChanged += (_, arg) =>
                {
                    if (arg.PropertyName == nameof(EntryModel.IsChecked))
                        this.AddEntry(buyScenarioEntryKey, true);
                };

                currentCategory.Weapons.Add(weaponModel);
            };

            // Метод для добавления новой категории. Определяет новый stackpanel и создает текстовую метку
            void AddGroupSeparator(string text)
            {
                currentCategory = new WeaponCategoryModel() { Name = text };
                this.BuyMenuModel.Categories.Add(currentCategory);
            };

            AddGroupSeparator(Res.Pistols);
            AddWeapon("glock", Res.Pistol1);
            AddWeapon("elite", Res.Pistol2);
            AddWeapon("p250", Res.Pistol3);
            AddWeapon("fn57", Res.Pistol4);
            AddWeapon("deagle", Res.Pistol5);

            AddGroupSeparator(Res.Heavy);
            AddWeapon("nova", Res.Heavy1);
            AddWeapon("xm1014", Res.Heavy2);
            AddWeapon("mag7", Res.Heavy3);
            AddWeapon("m249", Res.Heavy4);
            AddWeapon("negev", Res.Heavy5);

            AddGroupSeparator(Res.SMGs);
            AddWeapon("mac10", Res.Smg1);
            AddWeapon("mp7", Res.Smg2);
            AddWeapon("ump45", Res.Smg3);
            AddWeapon("p90", Res.Smg4);
            AddWeapon("bizon", Res.Smg5);

            AddGroupSeparator(Res.Rifles);
            AddWeapon("famas", Res.Rifle1);
            AddWeapon("ak47", Res.Rifle2);
            AddWeapon("ssg08", Res.Rifle3);
            AddWeapon("aug", Res.Rifle4);
            AddWeapon("awp", Res.Rifle5);
            AddWeapon("g3sg1", Res.Rifle6);

            AddGroupSeparator(Res.Gear);
            AddWeapon("vest", Res.Gear1);
            AddWeapon("vesthelm", Res.Gear2);
            AddWeapon("taser", Res.Gear3);
            AddWeapon("defuser", Res.Gear4);

            AddGroupSeparator(Res.Grenades);
            AddWeapon("molotov", Res.Grenade1);
            AddWeapon("decoy", Res.Grenade2);
            AddWeapon("flashbang", Res.Grenade3);
            AddWeapon("hegrenade", Res.Grenade4);
            AddWeapon("smokegrenade", Res.Grenade5);

            this.BuyMenuModel = buyMenuModel;
        }

        void InitGameSettingsTab()
        {
            SettingsCategoryModel currentCategory = null;
            
            void AddGroupHeader(string header)
            {
                currentCategory = new SettingsCategoryModel()
                {
                    Name = header
                };
                this.SettingsCategoryModels.Add(currentCategory);
            }

            DynamicEntryModel RegisterEntry(string cmd, BindableBase controllerModel, bool needToggle)
            {
                DynamicEntryModel entryModel = new DynamicEntryModel(controllerModel)
                {
                    Key = cmd,
                    NeedToggle = needToggle
                };

                currentCategory.Items.Add(entryModel);
                return entryModel;
            }

            void AddIntervalCmdController(string cmd, double from, double to, double step, double defaultValue)
            {
                // Определим целочисленный должен быть слайдер или нет
                bool isInteger = from % 1 == 0 && to % 1 == 0 && step % 1 == 0;
                // И создадим модель представления
                IntervalControllerModel sliderVM = new IntervalControllerModel()
                {
                    From = from,
                    To = to,
                    Step = step
                };

                // Получим базовую модель представления
                DynamicEntryModel entryModel = RegisterEntry(cmd, sliderVM, true);

                void HandleSliderValue(double value)
                {
                    string formatted = Executable.FormatNumber(value, isInteger);
                    Executable.TryParseDouble(formatted, out double fixedValue);
                    fixedValue = isInteger ? ((int)fixedValue) : fixedValue;

                    entryModel.Content = new SingleCmd($"{cmd} {formatted}").ToString();
                    entryModel.Arg = fixedValue;
                    //resultCmdBlock.Tag = fixedValue;

                    //if ((bool)checkbox.IsChecked) // Добавляем в конфиг только если это сделал сам пользователь
                    this.AddEntry(cmd, true);
                }

                sliderVM.PropertyChanged += (sender, arg) =>
                {
                    if (arg.PropertyName == nameof(IntervalControllerModel.Value))
                        HandleSliderValue(sliderVM.Value);
                };

                // обработчик интерфейса
                this.entryControllers.Add(new EntryController()
                {
                    Model = entryModel,
                    Focus = () =>
                    {
                        //gameSettingsTabButton.IsChecked = true;
                        entryModel.IsFocused = true;
                    },
                    Restore = () =>
                    {
                        // Сперва сбрасываем чекбокс, это важно
                        entryModel.IsChecked = false;
                        sliderVM.Value = defaultValue;
                        entryModel.Arg = defaultValue;
                    },
                    Generate = () =>
                    {
                        if (entryModel.Arg is double)
                        {
                            return new ParametrizedEntry<double>()
                            {
                                PrimaryKey = cmd,
                                Cmd = new SingleCmd(entryModel.Content),
                                IsMetaScript = false,
                                Type = EntryType.Dynamic,
                                Arg = (double)entryModel.Arg
                            };
                        }
                        else
                        {
                            return new ParametrizedEntry<double[]>()
                            {
                                PrimaryKey = cmd,
                                Cmd = new SingleCmd(entryModel.Content),
                                IsMetaScript = false,
                                Type = EntryType.Dynamic,
                                Arg = (double[])entryModel.Arg
                            };
                        }
                    },
                    UpdateUI = (entry) =>
                    {
                        entryModel.IsChecked = true;
                        if (entry is IParametrizedEntry<double>)
                        {
                            IParametrizedEntry<double> extendedEntry = (IParametrizedEntry<double>)entry;
                            sliderVM.Value = extendedEntry.Arg;
                            entryModel.Arg = extendedEntry.Arg;
                        }
                        else
                        {
                            IParametrizedEntry<double[]> extendedEntry = (IParametrizedEntry<double[]>)entry;
                            entryModel.Content = Executable.GenerateToggleCmd(
                                cmd, extendedEntry.Arg, isInteger).ToString();
                            entryModel.Arg = extendedEntry.Arg;
                        }
                    },
                    HandleState = (state) => entryModel.IsEnabled = state != EntryStateBinding.InvalidState
                });

                // Задаем начальное значение и тут же подключаем обработчика интерфейса
                sliderVM.Value = defaultValue;
                // Вручную вызовем метод для обновления выводимой команды, если стандартное значение равно 0
                if (defaultValue == 0)
                    HandleSliderValue(defaultValue);
            };

            void AddComboboxCmdController(string cmd, string[] names, int defaultIndex, bool isIntegerArg, int baseIndex = 0)
            {
                ComboBoxControllerModel comboboxController = new ComboBoxControllerModel();
                DynamicEntryModel entryModel = RegisterEntry(cmd, comboboxController, isIntegerArg);

                // Если надо предусмотреть функцию toggle, то расширяем сетку и добавляем кнопку
                if (isIntegerArg)
                {
                    //toggleButton.Click += (_, __) =>
                    //{
                    //    ToggleWindow toggleWindow = new ToggleWindow(true, 0, names.Length - 1);
                    //    if ((bool)toggleWindow.ShowDialog())
                    //    {
                    //        int[] values = toggleWindow.GeneratedArg.Split(' ').Select(n => int.Parse(n)).ToArray();
                    //        resultCmdBlock.Text = Executable.GenerateToggleCmd(cmd, values).ToString();
                    //        // Сохраним аргумент в теге
                    //        resultCmdBlock.Tag = values;

                    //        //if ((bool)checkbox.IsChecked) // Добавляем в конфиг только если это сделал сам пользователь
                    //        this.AddEntry(cmd, true);
                    //    }
                    //    else
                    //    {

                    //    }
                    //};
                }

                // Зададим элементы комбобокса
                names.ToList().ForEach(name => comboboxController.Items.Add(name));

                // Создадим обработчика пораньше, т.к. он понадобится уже при задании начального индекса комбобокса
                entryControllers.Add(new EntryController()
                {
                    Model = entryModel,
                    Focus = () =>
                    {
                        //gameSettingsTabButton.IsChecked = true;
                        entryModel.IsFocused = true;
                    },
                    Restore = () =>
                    {
                        // Сначала сбрасываем чекбокс, ибо дальше мы с ним сверяемся
                        entryModel.IsChecked  = false;
                        // искусственно сбрасываем выделенный элемент
                        comboboxController.SelectedIndex = -1;
                        // и гарантированно вызываем обработчик SelectedIndexChanged
                        comboboxController.SelectedIndex = defaultIndex;
                    },
                    Generate = () =>
                    {
                        SingleCmd resultCmd = new SingleCmd(entryModel.Content);

                        if (entryModel.Arg is int)
                        {
                            return new ParametrizedEntry<int>()
                            {
                                PrimaryKey = cmd,
                                Type = EntryType.Dynamic,
                                IsMetaScript = false,
                                Cmd = resultCmd,
                                Arg = (int)entryModel.Arg
                            };
                        }
                        else
                        {
                            return new ParametrizedEntry<int[]>()
                            {
                                PrimaryKey = cmd,
                                Type = EntryType.Dynamic,
                                IsMetaScript = false,
                                Cmd = resultCmd,
                                Arg = (int[])entryModel.Arg
                            };
                        }
                    },
                    UpdateUI = (entry) =>
                    {
                        entryModel.IsChecked = true;

                        if (entry is IParametrizedEntry<int>)
                        {
                            IParametrizedEntry<int> extendedEntry = (IParametrizedEntry<int>)entry;
                            comboboxController.SelectedIndex = extendedEntry.Arg;
                            entryModel.Arg = extendedEntry.Arg;
                        }
                        else
                        {
                            IParametrizedEntry<int[]> extendedEntry = (IParametrizedEntry<int[]>)entry;
                            entryModel.Content = Executable.GenerateToggleCmd(cmd, extendedEntry.Arg).ToString();
                            entryModel.Arg = extendedEntry.Arg;
                        }
                    },
                    HandleState = (state) => entryModel.IsEnabled = state != EntryStateBinding.InvalidState
                });

                comboboxController.PropertyChanged += (_, arg) =>
                {
                    if (arg.PropertyName == nameof(ComboBoxControllerModel.SelectedIndex))
                    {
                        if (comboboxController.SelectedIndex == -1) return;

                        string resultCmdStr;
                        if (isIntegerArg)
                            resultCmdStr = $"{cmd} {comboboxController.SelectedIndex + baseIndex}";
                        else
                            resultCmdStr = $"{cmd} {comboboxController.SelectedItem}";

                        entryModel.Content = new SingleCmd(resultCmdStr).ToString();
                        entryModel.Arg = comboboxController.SelectedIndex;
                        //if ((bool)checkbox.IsChecked) // Добавляем в конфиг только если это сделал сам пользователь
                        this.AddEntry(cmd, true);
                    }
                };

                // Команда по умолчанию обновится, т.к. уже есть обработчик
                comboboxController.SelectedIndex = defaultIndex;
            };

            void AddTextboxNumberCmdController(string cmd, double defaultValue, bool asInteger)
            {
                string formattedDefaultStrValue = Executable.FormatNumber(defaultValue, asInteger);
                double coercedDefaultValue = Executable.CoerceNumber(defaultValue, asInteger);

                TextboxControllerModel textboxModel = new TextboxControllerModel();
                DynamicEntryModel entryModel = RegisterEntry(cmd, textboxModel, true);

                //toggleButton.Click += (_, __) =>
                //{
                //ToggleWindow toggleWindow = new ToggleWindow(asInteger, double.MinValue, double.MaxValue);
                //if ((bool)toggleWindow.ShowDialog())
                //{
                //    double[] values = toggleWindow.GeneratedArg.Split(' ').Select(value =>
                //    {
                //        Executable.TryParseDouble(value, out double parsedValue);
                //        return parsedValue;
                //    }).ToArray();

                //    resultCmdBlock.Text = Executable.GenerateToggleCmd(cmd, values, asInteger).ToString();
                //    // Сохраним аргумент в теге
                //    resultCmdBlock.Tag = values;

                //    //if ((bool)checkbox.IsChecked) // Добавляем в конфиг только если это сделал сам пользователь
                //    this.AddEntry(cmd, true);
                //}
                //else
                //{

                //}
                //};

                textboxModel.PropertyChanged += (_, arg) =>
                {
                    if (arg.PropertyName == nameof(TextboxControllerModel.Text))
                    {
                        if (!Executable.TryParseDouble(textboxModel.Text.Trim(), out double fixedValue))
                            return;
                        // Обрезаем дробную часть, если необходимо
                        fixedValue = asInteger ? (int)fixedValue : fixedValue;

                        // сохраним последнее верное значение в тег текстового блока
                        entryModel.Arg = fixedValue;

                        string formatted = Executable.FormatNumber(fixedValue, asInteger);
                        entryModel.Content = new SingleCmd($"{cmd} {formatted}").ToString();

                        //if ((bool)checkbox.IsChecked) // Добавляем в конфиг только если это сделал сам пользователь
                        AddEntry(cmd, true);
                    }
                };

                this.entryControllers.Add(new EntryController()
                {
                    Model = entryModel,
                    Focus = () =>
                    {
                        //gameSettingsTabButton.IsChecked = true;
                        entryModel.IsFocused = true;
                    },
                    Restore = () =>
                    {
                        entryModel.IsChecked = false;
                        textboxModel.Text = formattedDefaultStrValue;
                        entryModel.Arg = coercedDefaultValue;
                    },
                    Generate = () =>
                    {
                        SingleCmd generatedCmd = new SingleCmd(entryModel.Content);

                        if (entryModel.Arg is double)
                        {
                            return new ParametrizedEntry<double>()
                            {
                                PrimaryKey = cmd,
                                Cmd = generatedCmd,
                                Type = EntryType.Dynamic,
                                IsMetaScript = false,
                                Arg = (double)entryModel.Arg // Подтягиваем аргумент из тега
                            };
                        }
                        else
                        {
                            return new ParametrizedEntry<double[]>()
                            {
                                PrimaryKey = cmd,
                                Cmd = generatedCmd,
                                Type = EntryType.Dynamic,
                                IsMetaScript = false,
                                Arg = (double[])entryModel.Arg // Подтягиваем аргумент из тега
                            };
                        }
                    },
                    UpdateUI = (entry) =>
                    {
                        entryModel.IsChecked = true;

                        if (entry is IParametrizedEntry<double>)
                        {
                            IParametrizedEntry<double> extendedEntry = (IParametrizedEntry<double>)entry;
                            textboxModel.Text = Executable.FormatNumber(extendedEntry.Arg, asInteger);
                            entryModel.Arg = extendedEntry.Arg;
                        }
                        else
                        {
                            IParametrizedEntry<double[]> extendedEntry = (IParametrizedEntry<double[]>)entry;
                            double[] values = extendedEntry.Arg;

                            entryModel.Content = Executable.GenerateToggleCmd(cmd, values, asInteger).ToString();
                            entryModel.Arg = extendedEntry.Arg;
                        }
                    },
                    HandleState = (state) => entryModel.IsEnabled = state != EntryStateBinding.InvalidState
                });

                // Начальное значение
                textboxModel.Text = formattedDefaultStrValue;
            };

            void AddTextboxStringCmdController(string cmd, string defaultValue)
            {
                TextboxControllerModel textboxModel = new TextboxControllerModel();
                DynamicEntryModel entryModel = RegisterEntry(cmd, textboxModel, false);

                textboxModel.PropertyChanged += (_, arg) =>
                {
                    // Обернем в команду только название команды, т.к. аргументы в нижнем регистре не нужны
                    entryModel.Content = $"{new SingleCmd(cmd)} {textboxModel.Text}";

                    // Добавляем в конфиг только если это сделал сам пользователь
                    AddEntry(cmd, true);
                };

                this.entryControllers.Add(new EntryController()
                {
                    Model = entryModel,
                    Focus = () =>
                    {
                        //gameSettingsTabButton.IsChecked = true;
                        entryModel.IsFocused = true;
                    },
                    Restore = () =>
                    {
                        entryModel.IsChecked = false;
                        textboxModel.Text = defaultValue;
                    },
                    Generate = () =>
                    {
                        SingleCmd generatedCmd = new SingleCmd(entryModel.Content, false);

                        return new ParametrizedEntry<string>()
                        {
                            PrimaryKey = cmd,
                            Cmd = generatedCmd,
                            Type = EntryType.Dynamic,
                            IsMetaScript = false,
                            Arg = (string)textboxModel.Text // Подтягиваем аргумент из тега
                        };
                    },
                    UpdateUI = (entry) =>
                    {
                        entryModel.IsChecked = true;
                        IParametrizedEntry<string> extendedEntry = (IParametrizedEntry<string>)entry;
                        textboxModel.Text = extendedEntry.Arg;
                    },
                    HandleState = (state) => entryModel.IsEnabled = state != EntryStateBinding.InvalidState
                });

                // Начальное значение
                textboxModel.Text = defaultValue;
            };

            string[] toggleStrings = new string[] { Res.Off, Res.On };
            string[] primaryWeapons = new string[] { "weapon_m4a4", "weapon_ak47", "weapon_awp" };
            string[] secondaryWeapons = new string[] { "weapon_hkp2000", "weapon_glock", "weapon_deagle" };

            AddGroupHeader(Res.CategoryMouseSettings);
            AddTextboxNumberCmdController("sensitivity", 2.5, false);
            AddTextboxNumberCmdController("zoom_sensitivity_ratio_mouse", 1, false);
            AddComboboxCmdController("m_rawinput", toggleStrings, 1, true);
            AddComboboxCmdController("m_customaccel", toggleStrings, 0, true);
            AddIntervalCmdController("m_customaccel_exponent", 0.05, 10, 0.05, 1.05);

            AddGroupHeader(Res.CategoryClientCommands);
            AddComboboxCmdController("cl_autowepswitch", toggleStrings, 1, true);
            AddIntervalCmdController("cl_bob_lower_amt", 5, 30, 1, 21);
            AddIntervalCmdController("cl_bobamt_lat", 0.1, 2, 0.1, 0.4);
            AddIntervalCmdController("cl_bobamt_vert", 0.1, 2, 0.1, 0.25);
            AddIntervalCmdController("cl_bobcycle", 0.1, 2, 0.01, 0.98);
            AddTextboxNumberCmdController("cl_clanid", 0, true);
            AddComboboxCmdController("cl_color", new string[] { Res.Yellow, Res.Purple, Res.Green, Res.Blue, Res.Orange }, 0, true);
            AddComboboxCmdController("cl_dm_buyrandomweapons", toggleStrings, 1, true);
            AddComboboxCmdController("cl_draw_only_deathnotices", toggleStrings, 1, true);
            AddComboboxCmdController("cl_hud_color",
                new string[] { Res.White, Res.LightBlue, Res.Blue, Res.Purple, Res.Red, Res.Orange, Res.Yellow, Res.Green, Res.Aqua, Res.Pink, Res.Default }, 0, true, baseIndex: 1);
            AddComboboxCmdController("cl_hud_healthammo_style", new string[] { Res.Health_Style0, Res.Health_Style1 }, 0, true);
            AddIntervalCmdController("cl_hud_radar_scale", 0.8, 1.3, 0.1, 1);
            AddIntervalCmdController("cl_hud_background_alpha", 0, 1, 0.1, 1);
            AddComboboxCmdController("cl_hud_playercount_pos", new string[] { Res.Top, Res.Bottom }, 0, true);
            AddComboboxCmdController("cl_hud_playercount_showcount", new string[] { Res.ShowAvatars, Res.ShowCount }, 0, true);
            AddComboboxCmdController("cl_mute_enemy_team", toggleStrings, 0, true);
            AddTextboxNumberCmdController("cl_pdump", -1, true);
            AddComboboxCmdController("cl_radar_always_centered", toggleStrings, 0, true);
            AddIntervalCmdController("cl_radar_icon_scale_min", 0.4, 1, 0.01, 0.7);
            AddIntervalCmdController("cl_radar_scale", 0.25, 1, 0.01, 0.7);
            AddComboboxCmdController("cl_righthand", toggleStrings, 0, true);
            AddComboboxCmdController("cl_showfps", toggleStrings, 0, true);
            AddComboboxCmdController("cl_show_clan_in_death_notice", toggleStrings, 0, true);
            AddComboboxCmdController("cl_showpos", toggleStrings, 0, true);
            AddComboboxCmdController("cl_teammate_colors_show", toggleStrings, 0, true);
            AddComboboxCmdController("cl_teamid_overhead_always", toggleStrings, 0, true);
            AddIntervalCmdController("cl_timeout", 4, 30, 1, 30);
            AddComboboxCmdController("cl_use_opens_buy_menu", toggleStrings, 1, true);
            AddIntervalCmdController("cl_viewmodel_shift_left_amt", 0.5, 2, 0.05, 1.5);
            AddIntervalCmdController("cl_viewmodel_shift_right_amt", 0.25, 2, 0.05, 0.75);
            AddComboboxCmdController("cl_cmdrate", new string[] { "64", "128" }, 1, false);
            AddComboboxCmdController("cl_updaterate", new string[] { "64", "128" }, 1, false);

            AddGroupHeader(Res.CategoryCrosshair);
            AddComboboxCmdController("cl_crosshair_drawoutline", toggleStrings, 0, true);
            AddIntervalCmdController("cl_crosshair_dynamic_maxdist_splitratio", 0, 1, 0.1, 0.35);
            AddIntervalCmdController("cl_crosshair_dynamic_splitalpha_innermod", 0, 1, 0.01, 1);
            AddIntervalCmdController("cl_crosshair_dynamic_splitalpha_outermod", 0.3, 1, 0.01, 0.5);
            AddTextboxNumberCmdController("cl_crosshair_dynamic_splitdist", 7, true);
            AddIntervalCmdController("cl_crosshair_outlinethickness", 0.1, 3, 0.1, 1);
            AddTextboxNumberCmdController("cl_crosshair_sniper_width", 1, false);
            AddIntervalCmdController("cl_crosshairalpha", 0, 255, 1, 200);
            AddComboboxCmdController("cl_crosshairdot", toggleStrings, 1, true);
            AddComboboxCmdController("cl_crosshairgap", toggleStrings, 1, true);
            AddComboboxCmdController("cl_crosshair_t", toggleStrings, 1, true);
            AddComboboxCmdController("cl_crosshairgap_useweaponvalue", toggleStrings, 1, true);
            AddTextboxNumberCmdController("cl_crosshairsize", 5, false);
            AddIntervalCmdController("cl_crosshairstyle", 0, 5, 1, 2);
            AddTextboxNumberCmdController("cl_crosshairthickness", 0.5, false);
            AddComboboxCmdController("cl_crosshairusealpha", toggleStrings, 1, true);
            AddTextboxNumberCmdController("cl_fixedcrosshairgap", 3, false);

            AddGroupHeader(Res.CategoryOther);
            AddTextboxStringCmdController("say", "vk.com/exideprod");
            AddTextboxStringCmdController("say_team", "Hello world!");
            AddTextboxStringCmdController("exec", "config");
            AddTextboxNumberCmdController("rate", 786432, true);
            AddTextboxStringCmdController("connect", "12.34.56.78:27015");
            AddTextboxStringCmdController("map", "de_mirage");
            AddTextboxStringCmdController("echo", "blog.exideprod.com");
            AddTextboxStringCmdController("record", "demo_name");
            AddComboboxCmdController("developer", toggleStrings, 0, true);
            AddComboboxCmdController("gameinstructor_enable", toggleStrings, 0, true);
            AddComboboxCmdController("r_drawothermodels", new string[] { "Off", "Normal", "Wireframe" }, 1, true);
            AddTextboxStringCmdController("host_writeconfig", "config_name");
            AddIntervalCmdController("mat_monitorgamma", 1.6, 2.6, 0.1, 2.2);
            AddTextboxNumberCmdController("mm_dedicated_search_maxping", 150, true);
            AddTextboxNumberCmdController("host_timescale", 1, false);
            AddTextboxNumberCmdController("demo_timescale", 1, false);

            AddGroupHeader(Res.CategoryServer);
            AddTextboxNumberCmdController("sv_airaccelerate", 12, true);
            AddTextboxNumberCmdController("sv_accelerate", 5.5, false);
            AddComboboxCmdController("sv_showimpacts", new string[] { "Off", "Server/client", "Client only" }, 0, true);
            AddTextboxNumberCmdController("mp_restartgame", 1, true);
            AddComboboxCmdController("mp_solid_teammates", toggleStrings, 0, true);
            AddComboboxCmdController("mp_ct_default_primary", primaryWeapons, 0, false);
            AddComboboxCmdController("mp_t_default_primary", primaryWeapons, 1, false);
            AddComboboxCmdController("mp_ct_default_secondary", secondaryWeapons, 0, false);
            AddComboboxCmdController("mp_t_default_secondary", secondaryWeapons, 1, false);

            AddGroupHeader(Res.CategoryVoiceAndSound);
            AddIntervalCmdController("volume", 0, 1, 0.05, 1);
            AddComboboxCmdController("voice_enable", toggleStrings, 1, true);
            AddComboboxCmdController("voice_loopback", toggleStrings, 1, true);
            AddIntervalCmdController("voice_scale", 0, 1, 0.05, 1);
            AddIntervalCmdController("snd_roundstart_volume", 0, 1, 0.05, 1);
            AddIntervalCmdController("snd_roundend_volume", 0, 1, 0.05, 1);
            AddIntervalCmdController("snd_tensecondwarning_volume", 0, 1, 0.05, 1);
            AddIntervalCmdController("snd_deathcamera_volume", 0, 1, 0.05, 1);

            AddGroupHeader(Res.CategoryNetGraphSettings);
            AddComboboxCmdController("net_graph", toggleStrings, 0, true);
            AddTextboxNumberCmdController("net_graphheight", 64, true);
            AddTextboxNumberCmdController("net_graphpos", 1, true);
            AddComboboxCmdController("net_graphproportionalfont", toggleStrings, 0, true);

            AddGroupHeader(Res.CategoryBots);
            AddComboboxCmdController("bot_stop", toggleStrings, 0, true);
            AddComboboxCmdController("bot_mimic", toggleStrings, 0, true);
            AddComboboxCmdController("bot_crouch", toggleStrings, 0, true);
            AddIntervalCmdController("bot_mimic_yaw_offset", 0, 180, 5, 0);
        }

        void ReadConfig()
        {
            // Делаем поиск по ресурсам регистронезависимым
            Res.ResourceManager.IgnoreCase = true;

            // Возьмем данные из конфига, если такой есть. Иначе создадим
            XmlSerializer ser = new XmlSerializer(typeof(AppConfig));

            if (!File.Exists(this.cfgPath))
            {
                using (FileStream fs = File.OpenWrite(this.cfgPath))
                {
                    AppConfig defaultCfg = new AppConfig()
                    {
                        CfgName = this.CustomCfgName,
                        CsgoCfgPath = this.CustomCfgPath
                    };
                    ser.Serialize(fs, defaultCfg);
                }
            }
            // К этому моменту у нас гарантированно создан конфигурационный файл
            using (FileStream fs = File.OpenRead(this.cfgPath))
            {

                this.cfg = (AppConfig)ser.Deserialize(fs);

                this.CustomCfgName = this.cfg.CfgName;
                this.CustomCfgPath = this.cfg.CsgoCfgPath;
            }

            // Добавляем слушателя на нажатие виртуальной клавиатуры
            //this.kb.OnKeyboardKeyDown += KeyboardKeyDownHandler;

            // Пусть коллекция сама добавляет слушатель нажатия новым элементам
            this.entryControllers.CollectionChanged += (_, arg) =>
            {
                if (arg.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                {
                    EntryController controller = (EntryController)arg.NewItems[0];
                    EntryModel entryModel = controller.Model;
                    entryModel.Click += (__, ___) => HandleEntryClick(entryModel.Key);
                }
            };

            

            // Инициализируем панели
            //this.keyDownAttachmentsContentControl.Content = keyDownAttachments;
            //this.keyUpAttachmentsContentControl.Content = keyUpAttachmentsVM;
            //this.solidAttachmentsContentControl.Content = solidAttachments;

            

            // Проверим, что в словаре нет одинаковых ключей в разном регистре
            // Для этого сгруппируем все ключи, переведя их в нижний регистр,
            // и найдем группу, где больше 1 элемента
            var duplicatedKeyGroup = this.entryControllers.GroupBy(c => c.Model.Key.ToLower())
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicatedKeyGroup != null) throw new Exception($"Duplicate key: {duplicatedKeyGroup.Key}");

            // Зададим привязку по умолчанию
            this.SetStateAndUpdateUI(EntryStateBinding.Default);
        }

        public void SaveConfig()
        {
            // Удалим прошлый файл
            File.Delete(this.cfgPath);
            // И создадим новый
            using (FileStream fs = File.OpenWrite(this.cfgPath))
            {
                XmlSerializer ser = new XmlSerializer(typeof(AppConfig));
                this.cfg.CfgName = this.CustomCfgName;
                this.cfg.CsgoCfgPath = this.CustomCfgPath;
                ser.Serialize(fs, this.cfg);
            }
        }

        string Localize(string primaryKey)
        {
            string result = Res.ResourceManager.GetString(primaryKey);
            return result ?? primaryKey;
        }

        string GeneratePrefix()
        {
            System.Text.StringBuilder prefixBuilder = new System.Text.StringBuilder();

            switch (this.StateBinding)
            {
                case EntryStateBinding.KeyDown:
                    {
                        prefixBuilder.Append($"{this.currentKeySequence.ToString()}_");
                        prefixBuilder.Append("down");
                        break;
                    }
                case EntryStateBinding.KeyUp:
                    {
                        prefixBuilder.Append($"{this.currentKeySequence.ToString()}_");
                        prefixBuilder.Append("up");
                        break;
                    }
                case EntryStateBinding.Default:
                    {
                        prefixBuilder.Append("default");
                        break;
                    }
                case EntryStateBinding.Alias:
                    {
                        //string aliasName = (aliasSetVM.GetSelectedItem().Text.ToString());
                        //prefixBuilder.Append($"{aliasName}");
                        break;
                    }
                default:
                    throw new Exception($"Попытка генерации префикса при состоянии {this.StateBinding}");
            }

            return prefixBuilder.ToString();
        }

        void AddEntry(string cfgEntryKey, bool abortIfNotUser)
        {
            EntryController controller = this.GetController(cfgEntryKey);

            // Если сказано, что отмена, если добавление идет не из-за действий пользователя
            // То значит гарантированно AttachedCheckbox не может быть равен null
            if (abortIfNotUser && !controller.Model.IsChecked) return;

            Entry generatedEntry = (Entry)controller.Generate();
            this.AddEntry(generatedEntry);
        }

        void HandleEntryClick(string entryKey)
        {
            // Получим обработчика и 
            EntryController entryController = this.GetController(entryKey);
            EntryModel entryModel = entryController.Model;
            Entry entry = (Entry)entryController.Generate();

            if (entryModel.IsChecked)
                this.AddEntry(entry);
            else
                this.RemoveEntry(entry);

            // Обновим панели
            this.UpdateAttachmentPanels();
        }

        void SetStateAndUpdateUI(EntryStateBinding newState)
        {
            this.StateBinding = newState;

            foreach (var controller in this.entryControllers)
                controller.HandleState(this.StateBinding);

            KeyDownAttachments.IsSelected = newState == EntryStateBinding.KeyDown;
            KeyUpAttachments.IsSelected = newState == EntryStateBinding.KeyUp;
            SolidAttachments.IsSelected = newState != EntryStateBinding.InvalidState;
        }

        BindEntry ConvertToBindEntry(Entry entry)
        {
            BindEntry bindEntry = (BindEntry)entry;

            Executable onKeyDown, onKeyUp;

            if (entry.IsMetaScript)
            {
                onKeyDown = new SingleCmd($"+{entry.Cmd}");
                onKeyUp = new SingleCmd($"-{entry.Cmd}");
            }
            else
            {
                bool isKeyDownBinding = this.StateBinding == EntryStateBinding.KeyDown;

                onKeyDown = isKeyDownBinding ? entry.Cmd : null;
                onKeyUp = isKeyDownBinding ? null : entry.Cmd;
            }

            bindEntry.OnKeyDown = onKeyDown;
            bindEntry.OnKeyRelease = onKeyUp;

            return bindEntry;
        }

        void RemoveEntry(string cfgEntryKey)
        {
            Entry generatedEntry = (Entry)this.GetController(cfgEntryKey).Generate();
            this.RemoveEntry(generatedEntry);
        }

        void AddEntry(Entry entry)
        {
            switch (this.StateBinding)
            {
                case EntryStateBinding.KeyDown:
                case EntryStateBinding.KeyUp:
                    {
                        BindEntry bindEntry = this.ConvertToBindEntry(entry);
                        this.cfgManager.AddEntry(this.currentKeySequence, bindEntry);
                        break;
                    }
                case EntryStateBinding.Default:
                    {
                        this.cfgManager.AddEntry(entry);
                        break;
                    }
                case EntryStateBinding.Alias:
                    {
                        // Проверяем, что добавляется не основной узел со всеми алиасами
                        //if (entry.PrimaryKey != aliasSetVM.Key)
                        //{
                        //    // Добавляем текущий элемент к коллекции, привязанной к выбранной кнопке
                        //    ItemViewModel selectedItem = aliasSetVM.GetSelectedItem();
                        //    List<Entry> attachedToAlias = (List<Entry>)(selectedItem.Tag);

                        //    attachedToAlias.Add(entry);
                        //    selectedItem.Tag = attachedToAlias;

                        //    // И вызываем обработчика пользовательских алиасов
                        //    Entry aliasSetEntry = (Entry)this.GetController(aliasSetVM.Key).Generate();
                        //    this.cfgManager.AddEntry(aliasSetEntry);
                        //}
                        //else
                        //{
                        //    // Если это основной узел, то добавим его напрямую в конфиг
                        //    this.cfgManager.AddEntry(entry);
                        //}
                        break;
                    }
                default: throw new Exception($"Состояние {this.StateBinding} при добавлении элемента");
            }
        }

        void RemoveEntry(Entry entry)
        {
            switch (this.StateBinding)
            {
                case EntryStateBinding.KeyDown:
                case EntryStateBinding.KeyUp:
                    {
                        BindEntry bindEntry = this.ConvertToBindEntry(entry);
                        this.cfgManager.RemoveEntry(this.currentKeySequence, bindEntry);
                        break;
                    }
                case EntryStateBinding.Default:
                    {
                        this.cfgManager.RemoveEntry(entry);
                        break;
                    }
                case EntryStateBinding.Alias:
                    {
                        //if (entry.PrimaryKey != aliasSetVM.Key)
                        //{
                        //    // Добавляем текущий элемент к коллекции, привязанной к выбранной кнопке
                        //    ItemViewModel selectedItem = aliasSetVM.GetSelectedItem();
                        //    List<Entry> attachedToAlias = ((List<Entry>)selectedItem.Tag)
                        //        .Where(e => e.PrimaryKey != entry.PrimaryKey).ToList();

                        //    selectedItem.Tag = attachedToAlias;

                        //    // Напрямую обновим узел в менеджере
                        //    Entry aliasSetEntry = (Entry)this.GetController(aliasSetVM.Key).Generate();
                        //    this.cfgManager.AddEntry(aliasSetEntry);
                        //}
                        //else
                        //{
                        //    // Если удаляем основной узел со всеми алиасами, то напрямую стираем его из менеджера
                        //    this.cfgManager.RemoveEntry(entry);
                        //}
                        break;
                    }
                default: throw new Exception($"Состояние {this.StateBinding} при попытке удалить элемент");

            }
        }

        EntryController GetController(string cmd)
        {
            return this.entryControllers.FirstOrDefault(c => c.Model.Key == cmd);
        }

        void ResetAttachmentPanels()
        {
            // Получим предыдущие элементы и сбросим связанные с ними элементы интерфейса
            // Для этого объединим коллекции элементов из всех панелей
            List<AttachmentsModel> attachments = new List<AttachmentsModel>()
            {
                this.KeyDownAttachments,
                this.KeyUpAttachments,
                this.SolidAttachments
            };

            IEnumerable<string> shownEntryKeys = attachments.SelectMany(model => model.Items.Select(i => i.Tag)).Cast<string>();

            foreach (string entryKey in shownEntryKeys)
            {
                EntryController controller = this.GetController(entryKey);
                // Метод, отвечающий непосредственно за сброс состояния интерфейса
                controller.Restore();
            }

            // Очистим панели
            attachments.ForEach(vm => vm.Items.Clear());
        }

        ///// <summary>
        ///// Метод для обновления панелей с привязанными к сочетанию клавиш элементами конфига
        ///// </summary>
        void UpdateAttachmentPanels()
        {
            // Очистим панели и сбросим настройки интерфейса
            ResetAttachmentPanels();

            // Локальный метод для добавления нового элемента в заданную панель
            void AddAttachment(string entryKey, AttachmentsModel attachmentsVM)
            {
                ItemModel item = new ItemModel()
                {
                    Text = Localize(entryKey),
                    Tag = entryKey
                };
                attachmentsVM.Items.Add(item);
            }

            // Согласно текущему состоянию выберем элементы
            if (this.StateBinding == EntryStateBinding.KeyDown || this.StateBinding == EntryStateBinding.KeyUp)
            {
                if (!this.cfgManager.Entries.TryGetValue(this.currentKeySequence, out List<BindEntry> attachedEntries)) return;

                /*
                 * Мета-скрипты привязываются только к нажатию кнопок (хотя отжатие обработано тоже)
                 * В других случаях может быть обработано либо нажатие, либо отжатие кнопки
                 */

                // Теперь заполним панели новыми элементами
                attachedEntries.ForEach(entry =>
                {
                    // Добавим в нужную панель
                    if (entry.OnKeyDown != null)
                        AddAttachment(entry.PrimaryKey, KeyDownAttachments);
                    else
                        AddAttachment(entry.PrimaryKey, KeyUpAttachments);

                    // Обновим интерфейс согласно элементам, привязанным к текущему состоянию
                    attachedEntries.Where(e => this.IsEntryAttachedToCurrentState(e))
                        .ToList().ForEach(e => this.GetController(e.PrimaryKey).UpdateUI(e));
                });
            }
            else if (this.StateBinding == EntryStateBinding.Default)
            {
                // Получаем все элементы по умолчанию, которые должны быть отображены в панели
                List<Entry> attachedEntries = this.cfgManager.DefaultEntries
                    .Where(e => this.GetController(e.PrimaryKey).Model.IsSelectable).ToList();

                // Теперь заполним панели новыми элементами
                attachedEntries.ForEach(entry =>
                {
                    AddAttachment(entry.PrimaryKey, SolidAttachments);
                    // Обновим интерфейс согласно элементам, привязанным к текущему состоянию
                    this.GetController(entry.PrimaryKey).UpdateUI(entry);
                });
            }
            else if (this.StateBinding == EntryStateBinding.Alias)
            {
                //int selectedIndex = this.aliasSetVM.GetFirstSelectedIndex();

                //if (selectedIndex == -1) return;

                //// Узнаем какие элементы привязаны к текущей команде
                //List<Entry> attachedEntries = (List<Entry>)(this.aliasSetVM.Items[selectedIndex].Tag);

                //attachedEntries.ForEach(entry =>
                //{
                //    AddAttachment(entry.PrimaryKey, solidAttachments);
                //    // Обновим интерфейс согласно элементам, привязанным к текущему состоянию
                //    this.GetController(entry.PrimaryKey).UpdateUI(entry);
                //});
            }
            else { } // InvalidState
        }

        private void GenerateConfig(object sender, RoutedEventArgs e)
        {
            if (this.CustomCfgName.Length == 0)
                this.CustomCfgName = "Config";

            string resultCfgPath = Path.Combine(GetTargetFolder(), $"{this.CustomCfgName}.cfg");

            this.cfgManager.GenerateCfg(resultCfgPath);
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{resultCfgPath}\"");
        }

        bool IsEntryAttachedToCurrentState(BindEntry entry)
        {
            if (this.StateBinding == EntryStateBinding.Default
                || this.StateBinding == EntryStateBinding.Alias
                || this.StateBinding == EntryStateBinding.InvalidState)
                throw new Exception(); // TODO: УБРАТЬ

            // Если сбрасываем привязанные к нажатию элементы, и у элемента определено действие на нажатие, то подходит
            if (this.StateBinding == EntryStateBinding.KeyDown && entry.OnKeyDown != null)
                return true;
            // Если привязанные к отжатию и у нас обрабатывается только отжатие, то подходит
            else if (this.StateBinding == EntryStateBinding.KeyUp && entry.OnKeyRelease != null && entry.OnKeyDown == null)
                return true;
            else
                return false;
        }

        void GenerateRandomCrosshairs(int count)
        {
            string prefix = GeneratePrefix();
            Random rnd = new Random();

            string GenerateRandomCmd(string cmd, double from, double to, bool asInteger)
            {
                double randomValue;

                if (!asInteger)
                    randomValue = from + rnd.NextDouble() * (to - from);
                else
                    randomValue = from + rnd.NextDouble() * (to - from + 1);

                string formatted = Executable.FormatNumber(randomValue, asInteger);
                return $"{cmd} {formatted}";
            };

            string firstCrosshair = null;

            for (int i = 0; i < count; i++)
            {
                string crosshairPath = $@"{GetTargetFolder()}\{prefix}_ch{i + 1}.cfg";
                if (firstCrosshair == null) firstCrosshair = crosshairPath;

                string[] lines = new string[]
                {
                    GenerateRandomCmd("cl_crosshair_drawoutline", 0, 1, true),
                    GenerateRandomCmd("cl_crosshair_outlinethickness", 1, 2, true),
                    GenerateRandomCmd("cl_crosshair_sniper_width", 1, 5, true),
                    GenerateRandomCmd("cl_crosshairalpha", 200, 255, true),
                    GenerateRandomCmd("cl_crosshairdot", 0, 1, true),
                    GenerateRandomCmd("cl_crosshairgap", 0, 1, true),
                    GenerateRandomCmd("cl_crosshair_t", 0, 1, true),
                    GenerateRandomCmd("cl_crosshairsize", -5, 5, true),
                    GenerateRandomCmd("cl_crosshairstyle", 0, 5, true),
                    GenerateRandomCmd("cl_crosshairthickness", 1, 3, true)
                };

                if (File.Exists(crosshairPath)) File.Delete(crosshairPath);
                File.WriteAllLines(crosshairPath, lines);
            }
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{firstCrosshair}\"");
        }

        void UpdateCfgManager()
        {
            // Сбросим все настройки от прошлого конфига
            foreach (EntryController controller in this.entryControllers)
                controller.Restore();

            // Зададим привязку к дефолтному состоянию
            //keyboardAliasCombobox.SelectedIndex = 0; // TODO:
            this.StateBinding = EntryStateBinding.Default;

            foreach (Entry entry in this.cfgManager.DefaultEntries)
                this.GetController(entry.PrimaryKey).UpdateUI(entry);

            this.UpdateAttachmentPanels();
            //this.ColorizeKeyboard(); // TODO:
        }

        string GetTargetFolder()
        {
            string cfgPath = this.CustomCfgPath;

            if (cfgPath.Length > 0 && Directory.Exists(cfgPath))
                return cfgPath;
            else
                return Directory.GetCurrentDirectory();
        }

        EntryStateBinding CoerceStateBinding(EntryStateBinding entryStateBinding)
        {
            //ComboBox cbox = (ComboBox)e.Source;
            //ComboBoxItem selectedItem = (ComboBoxItem)cbox.SelectedItem;

            if (entryStateBinding == EntryStateBinding.KeyDown)
            {
                // При выборе клавиатуры по умолчанию не выбрана последовательность
                return EntryStateBinding.InvalidState;
            }
            else
            {
                EntryStateBinding coercedState = entryStateBinding == EntryStateBinding.Default ?
                    EntryStateBinding.Default :
                    EntryStateBinding.Alias;

                this.SolidAttachments.Hint = coercedState == EntryStateBinding.Default ?
                    Res.CommandsByDefault_Hint :
                    Res.CommandsInAlias_Hint;

                this.currentKeySequence = null;
                //this.ColorizeKeyboard();

                if (coercedState == EntryStateBinding.Alias)
                {
                    // И при этом если ни одной команды не создано, то задаем неверное состояние
                    coercedState =
                        true ? //this.aliasSetVM.Items.Count == 0 ? // TODO
                        EntryStateBinding.InvalidState :
                        EntryStateBinding.Alias;
                }

                return coercedState;
            }
        }

        void HandleException(string userMsg, Exception ex)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.AppendLine(userMsg);

            Exception currentException = ex;

            do
            {
                builder.AppendLine($"{currentException.Message}");
                currentException = currentException.InnerException;
            } while (currentException != null);

            builder.Append($"StackTrace: {ex.StackTrace}");
            MessageBox.Show(builder.ToString());
        }
        //#endregion
    }
}
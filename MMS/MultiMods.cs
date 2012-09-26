﻿using System;
using System.IO;
using System.Collections.Generic;
using MMS.Properties;
using Common;

namespace MMS {
    /*
     * Manages the several available mods.
     */
    class MultiMods {
        static MultiMods instance;
        public static MultiMods Instance {
            get {
                if (instance == null) {
                    instance = new MultiMods();
                }
                return instance;
            }
        }

        private MultiMods() {
            if (!string.IsNullOrEmpty(Settings.Default.Mods)) {
                // retrieve mod list from settings
                string[] modNames = Settings.Default.Mods.Split(Path.PathSeparator);
                foreach (string modname in modNames) {
                    AddMod(modname, false);
                }
            } else {
                // added first mod... backup existing changes, but don't add to list
                Mod existing = new Mod("existing_changes");
                new ModDataSynchronizer(existing).BackupToMod();
                if (!existing.Accessor.IsEmpty("")) {
                    mods.Add(existing);
                }
            }

            // encode mod list in settings
            ModListChanged += delegate() {
                Settings.Default.Mods = string.Join(Path.PathSeparator.ToString(), ModNames);
            };

            // set previously active mod and set to settings for next start if changed
            CurrentMod = GetModByName(Settings.Default.ActiveMod);
            if (CurrentMod == null && Mods.Count == 1) {
                CurrentMod = Mods[0];
            }
            CurrentModSet += delegate() {
                Settings.Default.ActiveMod = CurrentMod.Name;
            };
        }

        #region Events
        public delegate void ModEvent();
        public event ModEvent ModListChanged;
        public event ModEvent CurrentModSet;
        #endregion

        #region Attributes
        List<Mod> mods = new List<Mod>();
        public List<Mod> Mods {
            get {
                return mods;
            }
        }
        public List<string> ModNames {
            get {
                List<string> result = new List<string>(mods.Count);
                mods.ForEach(mod => { result.Add(mod.Name); });
                return result;
            }
        }

        Mod currentMod;
        public Mod CurrentMod {
            get {
                return currentMod;
            }
            set {
                if (currentMod == value) {
                    return;
                }
                if (currentMod != null) {
                    currentMod.IsActive = false;
                }
                currentMod = value;
                if (currentMod != null) {
                    currentMod.IsActive = true;
                    if (CurrentModSet != null) {
                        CurrentModSet();
                    }
                }
            }
        }
        #endregion

        #region Mod List Change and Query Methods
        public Mod AddMod(string mod, bool setActive = true) {
            Mod result = null;
            if (!string.IsNullOrEmpty(mod)) {
                result = GetModByName(mod);
                if (result == null) {
                    result = new Mod(mod);
                    mods.Add(result);
                    if (setActive) {
                        CurrentMod = result;
                    }
                    if (ModListChanged != null) {
                        ModListChanged();
                    }
                }
            }
            return result;
        }

        /*
         * Remove the given mod from the list.
         */
        public void DeleteMod(Mod mod, bool deleteData = false) {
            if (mods.Remove(mod)) {
                if (mod == CurrentMod) {
                    // prevent the mod from backing up its data upon deactivation
                    // if it is the current mod
                    currentMod = null;
                }
                ModListChanged();
                if (deleteData) {
                    Directory.Delete(mod.ModDirectory, true);
                }
            }
        }

        // retrieve mod with the given name; null if not present
        Mod GetModByName(string name) {
            Mod result = null;
            foreach (Mod mod in mods) {
                if (mod.Name.Equals(name)) {
                    result = mod;
                    break;
                }
            }
            return result;
        }
        #endregion
    }
}

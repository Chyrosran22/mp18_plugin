using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Receiver2;
using Receiver2ModdingKit;
using Receiver2ModdingKit.CustomSounds;
using UnityEngine;
using UnityEngine.Assertions.Must;

namespace mp18_plugin
{
    public class MP18script : ModGunScript
    {
        public override LocaleTactics GetGunTactics()
        {
            return new LocaleTactics()
            {
                gun_internal_name = this.InternalName,
                title = "Izhmash Baikal MP-18", 
                text = "The Baikal MP-18 is the legendary Soviet single-barreled rifle.\nThis gun behaves perfectly in all weather conditions;\nno breakdowns or failures have ever been identified."
            };
        }
        public override ModHelpEntry GetGunHelpEntry()
        {
            return help_entry = new ModHelpEntry("MP18")
            {
                info_sprite = spawn_info_sprite,
                title = "Baikal MP-18",
                description = "Baikal MP-18 single-shot rifle\n"
                            + "Capacity: 1, 7.62x54mmR (7N1/PS GZh)\n"
                            + "\n"
                            + "Originally called the IZh-18 and produced by Izhmekh, this legendary single-shot Soviet weapon from 1964 is nowadays known as the Baikal MP-18 and is produced by Kalashnikov Concern. It has appeared in a wide range of calibres, from .223 to 9.4x73mm, as well as shotgun versions chambered in 12, 16, 20, 28, and 32 gauge, as well as .410 calibre. It is a common hunting weapon exported to many countries. This version is chambered in 7.62x54mmR; a commonly available cartridge with plenty of power and penetration.\n"
                            + "\n"
                            + "An extraordinarily simple break-open, single-shot rifle. Operation couldn't be easier; open, drop in bullet, close, and fire. Its simplicity gives rise to incredible reliability; jams or malfunctions are pretty much unheard of with this weapon. The worst that can happen is that it might occasionally fail to automatically eject spent cartridges upon opening. Comes with a simple cross-bolt safety button near the trigger."
            };
        }
        private FieldInfo m_yoke_open;
        private ModHelpEntry help_entry;
        private ChamberState chamber;
        private Vector3 bullet_eject_vector = new Vector3(0, 2, 0);
        [Range(0f,1f)]
        public float extractor_fail_chance = 0.05f;
        private bool number_rolled;
        LocalAimHandler lah;
        private bool checking_chamber;
        private float yoke_open
        {
            get {return (float)m_yoke_open.GetValue(this); }
            set { m_yoke_open.SetValue(this, value); }
        }
        public override CartridgeSpec GetCustomCartridgeSpec()
        {
            return new CartridgeSpec()
            {
                extra_mass = 21.9f,
                mass = 9.8f,
                speed = 823f,
                diameter = 0.00792f
            };
        }
        public override void AwakeGun()
        {
            lah = LocalAimHandler.player_instance;

            this.m_yoke_open = typeof(GunScript).GetField("yoke_open", BindingFlags.Instance | BindingFlags.NonPublic);

            chamber = cylinder.chambers[0]; //ChamberState isn't static, sadge, we have to instanciate it ourselves, we know there's only 1 chamber, so it's the 0th one.
        }
        public override void UpdateGun()
        {
            var prev_hammer_amount = hammer.amount;
            if (yoke_open >= 0.69f && yoke_stage == YokeStage.Opening) //phew, yoke_open is a float, so we can just cast it as a float, get its value, and be on our way, also check if there's a cartridge present, otherwise it throws around a NullException.
            {
                if (!number_rolled && (!Probability.Chance(ExtractorFailToEjectProbability()) && !Plugin.force_failure_to_extract.Value) && chamber.HasBullet())
                {
                    chamber.BulletFallFromChamber(bullet_eject_vector);
                }
                number_rolled = true;
            }
            else
            {
                number_rolled=false;
            }
            this.hammer.asleep = true;
           
            if (IsSafetyOn())
            {
                trigger.amount = Mathf.Min(trigger.amount, 0.2f);
                trigger.UpdateDisplay();
            }

            if(this.hammer.amount == 1)
            {
                this._hammer_state = 2;
            }
            /*if(this.trigger.amount != 1 && this._hammer_state !=2)
            {
                this.hammer.amount = yoke_open;
            }*/
            if (this.trigger.amount != 1 && this._hammer_state != 2 && yoke_stage == YokeStage.Opening)
            {
                this.hammer.amount = Mathf.MoveTowards(hammer.amount, 1f, Time.deltaTime * 10f);
            }
            if(this.trigger.amount == 1)
            {
                this.hammer.asleep = false;
            }
            this.hammer.TimeStep(Time.deltaTime);
            this.hammer.UpdateDisplay();
            if(this.hammer.amount == 0 && this._hammer_state == 2 && !IsSafetyOn() && !(yoke_open > 0f))
            {
                this._hammer_state = 0;
                this.StrikeCylinder(0, 1);
            }
            safety.transform = (yoke_open > 0f) ? null : transform.Find("safety"); //prevents the safety from being switch while manually extracting a round
            if (yoke_open <= press_check_amount && player_input.GetButton(RewiredConsts.Action.Slide_Lock) && player_input.GetButtonDown(RewiredConsts.Action.Swing_Out_Cylinder))
            {
                checking_chamber = true;
                ModAudioManager.PlayOneShotAttached(sound_press_check_start, transform_cylinder.gameObject);
            }

            if (checking_chamber && !player_input.GetButton(RewiredConsts.Action.Slide_Lock))
            {
                checking_chamber = false;
            }

            if (checking_chamber)
            {
                yoke_open = Math.Min(yoke_open, press_check_amount);
            }
            this.ApplyTransform("breakopen_animation", yoke_open, this.transform.Find("cylinder"));
            UpdateAnimatedComponents();
            if (IsSafetyOn())
            {
                hammer.amount = prev_hammer_amount;
                hammer.UpdateDisplay();
            }
            if (safety.transform != null) safety.UpdateDisplay();   
        }
        private float ExtractorFailToEjectProbability()
        {
            return extractor_fail_chance;
        }
    }
}

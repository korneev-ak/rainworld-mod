using BepInEx;
using MoreSlugcats;
using static Player;
using UnityEngine;
using RWCustom;
using System.Reflection;




namespace MyMod
{
    [BepInEx.BepInPlugin(GUID, "qwerty123", "1.1.1")]
    public class Class1 : BaseUnityPlugin
    {
        private const string GUID = "123.123";
        private int SAndRCounter = 0;//Player.swallowAndRegurgitateCounter resets, so the field is keeping it for this.Player_regurgitate
        private bool wantToRegurgitate = false;//false - do orig Regurgitate(), true - modified Regurgitate()
        MethodInfo Grabability = typeof(Player).GetMethod(("Grabability"), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static); //taking private Player.Grabability(..)





        private void OnEnable()
        {
            On.Player.Update += Player_update;
            On.Player.Regurgitate += Player_regurgitate;
            On.Player.CanBeSwallowed += Player_canBeSwallowed;              
        }        

        private bool Player_canBeSwallowed(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
        {

            if (!(testObj is Rock) && !(testObj is DataPearl) && !(testObj is FlareBomb) && !(testObj is Lantern) && !(testObj is FirecrackerPlant) && (!(testObj is VultureGrub) || (testObj as VultureGrub).dead) && (!(testObj is Hazer) || (testObj as Hazer).dead || (testObj as Hazer).hasSprayed) && !(testObj is FlyLure) && !(testObj is ScavengerBomb) && !(testObj is PuffBall) && !(testObj is SporePlant) && !(testObj is BubbleGrass) && (!(testObj is SSOracleSwarmer) || self.FoodInStomach < self.MaxFoodInStomach) && !(testObj is NSHSwarmer) && !(testObj is OverseerCarcass) && (!ModManager.MSC || !(testObj is FireEgg) || self.FoodInStomach < self.MaxFoodInStomach))
            {
                if (ModManager.MSC && testObj is SingularityBomb && !(testObj as SingularityBomb).activateSingularity)
                {
                    return !(testObj as SingularityBomb).activateSucktion;
                }
                return false;
            }
            return true;
        }

        //replaces Player's Regurgitate() with only change of first condition
        private void Player_regurgitate(On.Player.orig_Regurgitate orig, Player self)
        {
            if (wantToRegurgitate == false)
            {
                orig(self);
                return;
            } 

            if (self.objectInStomach == null)
            {
                if (!self.isGourmand)
                {
                    return;
                }

                self.objectInStomach = GourmandCombos.RandomStomachItem(self);
            }

            self.room.abstractRoom.AddEntity(self.objectInStomach);
            self.objectInStomach.pos = self.abstractCreature.pos;
            self.objectInStomach.RealizeInRoom();
            if (ModManager.MMF && MMF.cfgKeyItemTracking.Value && AbstractPhysicalObject.UsesAPersistantTracker(self.objectInStomach) && self.room.game.IsStorySession)
            {
                (self.room.game.session as StoryGameSession).AddNewPersistentTracker(self.objectInStomach);
                if (self.room.abstractRoom.NOTRACKERS)
                {
                    self.objectInStomach.tracker.lastSeenRegion = self.lastGoodTrackerSpawnRegion;
                    self.objectInStomach.tracker.lastSeenRoom = self.lastGoodTrackerSpawnRoom;
                    self.objectInStomach.tracker.ChangeDesiredSpawnLocation(self.lastGoodTrackerSpawnCoord);
                }
            }

            Vector2 pos = self.bodyChunks[0].pos;
            Vector2 vector = Custom.DirVec(self.bodyChunks[1].pos, self.bodyChunks[0].pos);
            bool flag = false;
            if (Mathf.Abs(self.bodyChunks[0].pos.y - self.bodyChunks[1].pos.y) > Mathf.Abs(self.bodyChunks[0].pos.x - self.bodyChunks[1].pos.x) && self.bodyChunks[0].pos.y > self.bodyChunks[1].pos.y)
            {
                pos += Custom.DirVec(self.bodyChunks[1].pos, self.bodyChunks[0].pos) * 5f;
                vector *= -1f;
                vector.x += 0.4f * (float)self.flipDirection;
                vector.Normalize();
                flag = true;
            }

            self.objectInStomach.realizedObject.firstChunk.HardSetPosition(pos);
            self.objectInStomach.realizedObject.firstChunk.vel = Vector2.ClampMagnitude((vector * 2f + Custom.RNV() * UnityEngine.Random.value) / self.objectInStomach.realizedObject.firstChunk.mass, 6f);
            self.bodyChunks[0].pos -= vector * 2f;
            self.bodyChunks[0].vel -= vector * 2f;
            if (self.graphicsModule != null)
            {
                (self.graphicsModule as PlayerGraphics).head.vel += Custom.RNV() * UnityEngine.Random.value * 3f;
            }

            for (int i = 0; i < 3; i++)
            {
                self.room.AddObject(new WaterDrip(pos + Custom.RNV() * UnityEngine.Random.value * 1.5f, Custom.RNV() * 3f * UnityEngine.Random.value + vector * Mathf.Lerp(2f, 6f, UnityEngine.Random.value), waterColor: false));
            }

            self.room.PlaySound(SoundID.Slugcat_Regurgitate_Item, self.mainBodyChunk);
            if (self.objectInStomach.realizedObject is Hazer && self.graphicsModule != null)
            {
                (self.objectInStomach.realizedObject as Hazer).SpitOutByPlayer(PlayerGraphics.SlugcatColor(self.playerState.slugcatCharacter));
            }

            if (flag && self.FreeHand() > -1)
            {
                if (ModManager.MMF && ((self.grasps[0] != null) ^ (self.grasps[1] != null)) && (ObjectGrabability)Grabability.Invoke(self,new object[] { self.objectInStomach.realizedObject}) == ObjectGrabability.BigOneHand)
                {
                    int num = 0;
                    if (self.FreeHand() == 0)
                    {
                        num = 1;
                    }

                    if ((ObjectGrabability)Grabability.Invoke(self, new object[] { self.objectInStomach.realizedObject }) != ObjectGrabability.BigOneHand)
                    {
                        self.SlugcatGrab(self.objectInStomach.realizedObject, self.FreeHand());
                    }
                }
                else
                {
                    self.SlugcatGrab(self.objectInStomach.realizedObject, self.FreeHand());
                }
            }

            self.objectInStomach = null;
            
        }

        private void Player_update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            
            
            if (self.input[0].y == 1 && self.input[0].pckp==true && self.SlugCatClass==MoreSlugcatsEnums.SlugcatStatsName.Spear)
            {                
                self.swallowAndRegurgitateCounter = SAndRCounter;
                if (self.objectInStomach == null)
                {
                    for (int i = 0;i<self.grasps.Length;i++)
                    {
                        if (self.grasps[i] != null && self.CanBeSwallowed(self.grasps[i].grabbed))
                        {
                            if (self.swallowAndRegurgitateCounter > 100)
                            {
                                self.SwallowObject(i);
                                self.swallowAndRegurgitateCounter = 0;
                            }
                            self.swallowAndRegurgitateCounter++;
                        }
                    }                    
                }
                else
                {
                    if (self.swallowAndRegurgitateCounter > 100)
                    {
                        wantToRegurgitate = true;
                        self.Regurgitate();
                        wantToRegurgitate = false;
                        self.swallowAndRegurgitateCounter = 0;
                    }
                    self.swallowAndRegurgitateCounter++;
                }         
            }
            SAndRCounter = self.swallowAndRegurgitateCounter;


            
            

        }

        
    }
    
    
}


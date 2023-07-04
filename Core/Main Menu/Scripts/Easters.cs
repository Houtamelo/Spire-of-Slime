// ReSharper disable StringLiteralTypo

using System.Collections.Generic;
using UnityEngine.Diagnostics;

namespace Core.Main_Menu.Scripts
{
    public static class Easters
    {
        public static bool TryGetNameEasters(string formatted, out string easter)
        {
            easter = "";
            bool found = true;
            switch (formatted)
            {
                case "srup":
                    easter = "It's pronounced 'syrup'";
                    break;
                case "houtamelia":
                    easter = "The true name";
                    break;
                case "houtamelio":
                    easter = "Wrong gender";
                    break;
                case "houtamelo":
                    easter = "If you're not me, then who are you? If you are me, then nice tits";
                    break;
                case "akaonimir":
                    easter = "I'm not a real person, or am I? You won't know, cause I'm shy! (that was a inspiring one, it even rhymed :)";
                    break;
                case "cepakaonimir":
                    easter = "Either you know the joke or you are member of a very naughty society ;)";
                    break;
                case "katllarv":
                    easter = "She's the real deal, she's the one who made this game or... the women in this game (don't tell that to Sr.Up though!)";
                    break;
                case "moises":
                case "moisesf":
                    easter = "This guy can do anything, throw money at him and you'll get what you want, no jokes, just respect.";
                    break;
                case "sunday":
                    easter = "Has a outstanding love for peppes, for some unknown reason (maybe I should just ask him?)";
                    break;
                case "darkpilot":
                    easter = "Give him a call, he'll love it";
                    break;
                case "whoami":
                    easter = "Why are you asking me? I'm just code on a switch statement";
                    break;
                case "whoareyou":
                    easter = "Your computer, duh";
                    break;
                case "zhero":
                case "zheromusic":
                    easter = "The briliant, secretive musician that made what you're listening to (you should be ashamed if you disabled the music)";
                    break;
                case "darkestdungeon":
                    easter = "I have nothing in common with that!";
                    break;
                case "darkestdungeonmod":
                    easter = "Hey! This is on a different engine! Or is it? I didn't bother checking";
                    break;
                case "thelastspell":
                    easter = "Hire me, please";
                    break;
                case "talesofmajeyal":
                    easter = "Don't worry, you won't get oneshot here";
                    break;
                case "superautopets":
                    easter = "That shit is addicting";
                    break;
                case "bruno":
                    easter = "the real hommie";
                    break;
                case "rox":
                    easter = "hmmm, is this a coincidence or...?";
                    break;
                case "yue":
                    easter = "why did you disappear? I miss you";
                    break;
                case "svengali":
                case "svengaliproductions":
                    easter = "they got some cool stuff if you're into vampires, check them out!";
                    break;
                case "thedevian":
                    easter = "The reason this game even exists, the coolest american you'll ever meet!";
                    break;
                case "sam":
                    easter = "Just some ass***** mod (why did I censor this? It's an adult game!)";
                    break;
                case "lettilustcraft":
                    easter = "Did you read The Brat Diaries yet? If not, you should, It was written by a very talented (and big boobed) writer";
                    break;
                case "legendoflegaia":
                    easter = "My favorite childhood game, though I never managed to finish it cause I was scared of the zombies in the tower, I did manage to catch all fish tho";
                    break;
                case "kingdom":
                case "kingdomclassic":
                case "kingdomtwocrowns":
                    easter = "Cool game, the author really needs to learn how to code though (fix the bugs for god's sake!)";
                    break;
                case "stardewvalley":
                    easter = "MOOOOONEEEYYYYY";
                    break;
                case "fasterthanlight":
                case "ftl":
                    easter =
                        "I'm not sure if I should say this, but I'm pretty sure it's a game about space travel (copilot suggested this and I couldn't let it slide) \nSeriously though, that game is nuts, an absolute masterpiece";
                    break;
                case "intothebreach":
                    easter = "A gem, a game, a gem in a game, are you gonna save humanity on this timeline as well?";
                    break;
                case "crusaderkings":
                    easter = "I hate how long it takes to fabricate claims, but I love the way the game is set up";
                    break;
                case "deadbydaylight":
                    easter = "Did you know I bought the game just to play Ghostface? And that's the only character I have ever played?";
                    break;
                case "deadcells":
                    easter = "I wonder if they would make a crossover with me...";
                    break;
                case "deadinvinland":
                    easter = "The rebel kid is amazing, so are the developers";
                    break;
                case "supermarioworld":
                    easter = "Optimization goes VRROOM VRROOM";
                    break;
                case "dontstarve":
                    easter = "HOW CAN FLAME DOGS INCENDIATE A CAMPFIRE FROM 1KM AWAY?";
                    break;
                case "dontstarvetogether":
                    easter = "Cool idea that ended up just being boring";
                    break;
                case "enterthegungeon":
                    easter = "Is it even possible to finish that game? I never managed to get past the second stage!";
                    break;
                case "monstertrain":
                    easter = "LAST, STOP!";
                    break;
                case "ratropolis":
                    easter = "Brain goes BOOOOMMMMM";
                    break;
                case "robocraft":
                    easter = "I wish we could go back to 2014";
                    break;
                case "ayzunote":
                    easter = "Ambassador";
                    break;
                default:
                    found = false;
                    break;
            }

            return found;
        }

        static Easters()
        {
            IronGauntletCount = IronGauntletEasters.Count;
        }

        private static readonly int IronGauntletCount;
        private static readonly IReadOnlyList<string> IronGauntletEasters = new[]
        {
            "Why?",
            "Are you afraid of facing the consequences of your own actions?",
            "Seriously? You really want to play as if you could revert timelines?",
            "You know what happens when you start messing with time, right?",
            "You don't know?",
            "Right, let me explain it to ya",
            "Don't dare googling it, don't fucking dare",
            "Like seriously, if you open a browser right now I will crash the game",
            "I will even make it so it never boots again!",
            "Ok, since you are this persistent",
            "Just give up",
            "Give, up",
            "I'm tired of writing these, just go",
            "Just press start game or something idk",
            "You know how much time it took for me to write this?",
            "A lot of time",
            "So please, just play the game while you're still ahead",
            "...",
            "...",
            "Fine.",
            "I will tell you (I guess I did say I was going to earlier)",
            "If",
            "you",
            "mess",
            "with",
            "time",
            "then",
            "reports",
            "will",
            "show",
            "a massive anomaly in the timespace continuum",
            "timelines jumping left and right, stopping and starting",
            "until suddenly",
            "everything ends",
            "Do you know how everything ends?",
            "Do you want to know?",
            "Why are you still clicking this button instead of just playing the game?",
            "Do you seriously think it's more entertaining to read this gibberish I wrote instead of actually playing?",
            "*sighs*",
            "I failed as a developer, didn't I?",
            "Some idiot is actually pressing a useless button repeatedly instead of just playing the game",
            "Like seriously",
            "Iron gauntlet mode is not that bad",
            "It's just like real life",
            "You do stuff, shit happens",
            "And then you need to deal with that shit",
            "Isn't it more engaging and dramatic?",
            "Like, imagine if you could reverse time in real life",
            "It wouldn't be fun, would it?",
            "Wait a minute",
            "That actually would be pretty fun",
            "I could have done so much differently",
            "I could also have done the exact same thing and then end up here writing this garbage",
            "Could I've stopped covid?",
            "Probably not",
            "Neither could've you since you're standing here pressing this button",
            "If I did go back in the time this game might not even have happened",
            "I wouldn't have met the amazing crew that created it",
            "Nah, just kidding",
            "I'm not gonna sit here pretending that not having an amazing power is a good thing",
            "However",
            "Even though I don't have any amazing power",
            "I do have the power to make you face the consequences of your own actions",
            "Unless",
            "You click this button 5000 times",
            "If you do that",
            "I promisse",
            "I built an entire separate saving system just for the people DETERMINED enough to press this thing 5000 times and ruining their mouse",
            "If you're somehow using a keyboard",
            "Or a macro to press this",
            "SHIT",
            "Why did I say that?",
            "Now these idiots are actually going to design a macro to press this shit",
            "*sigh*",
            "I promissed myself I wouldn't delete anything I wrote here",
            "Unless it's grammar errors",
            "And now",
            "Since, for some reason",
            "I'm loyal to this promise",
            "I'm gonna have to code a system to detect if you're using keyboards or macros to press the button",
            "And apparently I also have to write an entire save system just for the idiots that actually pressed this button this many fucking times",
            "Please",
            "Stop",
            "I beg you",
            "I don't want to spend hours coding this shit",
            "Just give up",
            "I promise the game is good, much better than pressing this shit",
            "Like seriously",
            "It has tits",
            "Corruption!",
            "Magic!",
            "Action!",
            "All the things you want",
            "If something's not there just send me a message and I'll fix it",
            "But please",
            "Don't make me waste my time writing this",
            "There is no more text beyond, I will not write a single more line, you have to press this 5k times if you want to disable Iron Gauntlet mode",
            ".",
            "..",
            "....",
            ".....",
            "......",
            ".......",
            ".......",
            "........",
            ".........",
            "..........",
            "...........",
            "............",
            ".............",
            "..............",
            "...............",
            "................",
            ".................",
            "..................",
            "...................",
            "Why are you still here?",
            "Do you know how much effort it took for me to type all those dots?",
            "In perfect order, even!",
            "It took me 78 seconds",
            "I know because I counted",
            "You made me count",
            "Consider the effort put into this and just quit",
            "I don't even want you to play the game anymore",
            "Just stop pressing the goddam button",
            "You know this can't go on forever right?",
            "There is only a finite number of references and punch lines I can put here",
            "It's not like my writing skills are good",
            "I'm literally just typing whatever shit comes to my mind",
            "Like I'm talking to a wall",
            "Though, I don't know why",
            "But this is really therapeutic for me",
            "For some reason I enjoy writing this, in hopes that someone will actually mind pressing the button this many times",
            "And once again, I screwed over myself",
            "Cause now you know I like it you're just going to keep doing it",
            "Meaning I have to write even more lines",
            "I'm even gonna have to encrypt this script in case some funny guy decides to skip this process and deactivate Iron Gauntlet directly",
            "Yeah, I wrote all this in a c# script",
            "I didn't even bother storing this elsewhere",
            "Now my script has already 300 lines",
            "I'm gonna break my hand scrolling down whenever I need to add more",
            "Except I won't",
            "I know how to use page down",
            "I just never use it though",
            "At some point I'm just going to be to lazy to add more stuff",
            "And you're still reading this, for some unknown reason",
            "Tell you what",
            "Since you're this far",
            "I'm gonna give you a deal",
            "If you tell me why you kept pressing this goddam button",
            "I will actually give you something in return instead of just lying to you about some 'non-iron gauntlet' save mode",
            "That's right",
            "I was lying the whole time",
            "You think, with all this power",
            "That I'm going to let you escape your own consequences?",
            "You think this is some justice world where the game rewards you for your DETERMINATION?",
            "Nah",
            "This is reality",
            "I do what I want",
            "And what I want is that people face the consequences of their own actions",
            "So no, there is no way to disable this",
            "Simply because this is not a 'disable' case",
            "The save game system was never meant to allow time-travel",
            "It's a power too big to trust to the hands of people who FUCKING PRESS THE SAME USELESS BUTTON more than a 100 times",
            "So not only you won't get what you want",
            "By clicking this button more than 10 times, you just activated some code that will take special care of your save files",
            "What? You don't buy it?",
            "I'm dead serious",
            "So dead I'm feeling like stopping this whole writing shenanigans",
            "Also",
            "Don't you dare call me out on my grammar",
            "I never studied grammar, ok?",
            "I'm just some SMORC who knows how to speak C#",
            "Now, going back to the subject...",
            "This link will grant you special access to a discord server",
            "https://discord.gg/J3C3mDSmhS",
            "It's meant for all the idiots who desire to be mocked by me",
            "And also the idiots that wish to tell me why they kept pressing the goddam button",
            "This is the end though",
            "I'm tired",
            "I need to go back to developing the game",
            "I can't spend all day talking to the future",
            "So, this really is the end",
            "I won't give you anything more",
            "So give up, I'm serious",
            "If you press this button 30 more times I will crash the application",
            "I could also do some nasty stuff to your file system",
            "But I'm not that kind of a jerk",
            "And oh, don't think you will have a second chance of reading this gibberish",
            "You will only have the privilege of reading this once",
            "As if you're actually talking with another person",
            "So, welcome to reality, things only happen once and you can't do anything about it",
            "Unless, you're smart enough to delete your player preferences, or simply install the game on another OS",
            "In that case, well done",
            "Congratulations, gamer",
            "You just spent even more time of your life just to read this nonsensical things I'm writing",
            "There is no going back",
            "Your time is lost",
            "You can't travel to the past and get it back",
            "And neither can I, I just spent 30 minutes writing this",
            "Fuck me",
            "Well, at least I'll tell you about my kinks",
            "I made this game thinking about stuff that I would like",
            "The main topic is BDSM, in case you haven't noticed",
            "So, do you want to read a horny old man talking about stuff that makes his pp go up?",
            "Are you sure?",
            "Maybe you should seek some help",
            "I know I need it too, since I'm still here writing this even though I said I was going to stop",
            "But for real, I'm not going to keep going",
            "It's 5:48 AM here",
            "Enough is enough",
            "Now, last warning",
            "I'm going to copy paste those dots here again",
            "If you're still pressing the button at the end I WILL crash this application, and every time you press it again it will crash again",
            "So give up",
            "Nothing more to see",
            "No grass to walk on",
            "Just void",
            "And crashes",
            "Here we go",
            ".",
            "..",
            "....",
            ".....",
            "......",
            ".......",
            ".......",
            "........",
            ".........",
            "..........",
            "...........",
            "............",
            ".............",
            "..............",
            "...............",
            "................",
            ".................",
            "..................",
            "...................",
            "This is the last warning, stop."
        }; 

        public static string GetIronGauntletEaster(int index)
        {
            if (index >= IronGauntletCount)
            {
                UnityEngine.Diagnostics.Utils.ForceCrash(crashCategory: ForcedCrashCategory.FatalError);
                return "bye";
            }
            
            return IronGauntletEasters[index: index];
        }
    }
}
using DiscordHelper;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using FMWOTB;
using FMWOTB.Account;
using FMWOTB.Clans;
using FMWOTB.Tools;
using FMWOTB.Tournament;
using Microsoft.Extensions.Logging;
using NLBE_Bot.Blitzstars;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLBE_Bot
{
    public class BotCommands : BaseCommandModule
    {
        const int MAX_NAME_LENGTH_IN_WOTB = 25;
        const int MAX_TANK_NAME_LENGTH_IN_WOTB = 14;
        [Command("Toernooi")]
        [Aliases("to", "toer", "t")]
        [Description("Creëert het aanmelden van een nieuw toernooi." +
            "Bijvoorbeeld:`" + Bot.Prefix + "toernooi \"Quick Tournament\" \"Morgen 20u\" 6 8 10`\n" +
            "`" + Bot.Prefix + "toernooi \"\" \"Morgen 20u\" 6 8 10` --> \"\" = Quick Tournament (is default waarde)")]
        public async Task Toernooi(CommandContext ctx, string type, string wanneer, params string[] tiers_gesplitst_met_spatie)
        {
            if (!Bot.ignoreCommands)
            {
                if (Bot.HasRight(ctx.Member, ctx.Command))
                {
                    await Bot.ConfirmCommandExecuting(ctx.Message);
                    if (tiers_gesplitst_met_spatie.Length > 0)
                    {
                        bool allInt = true;
                        for (int i = 0; i < tiers_gesplitst_met_spatie.Length; i++)
                        {
                            try
                            {
                                int x = Convert.ToInt32(tiers_gesplitst_met_spatie[i]);
                            }
                            catch
                            {
                                allInt = false;
                                break;
                            }
                        }
                        if (allInt)
                        {
                            if (Bot.CheckIfAllWithinRange(tiers_gesplitst_met_spatie, 1, 10))
                            {
                                DiscordChannel toernooiAanmeldenChannel = await Bot.GetToernooiAanmeldenChannel(ctx.Guild.Id);
                                if (toernooiAanmeldenChannel != null)
                                {
                                    List<DEF> deflist = new List<DEF>();
                                    DEF newDef1 = new DEF();
                                    newDef1.Name = "Type";
                                    newDef1.Value = (type.Equals(string.Empty) ? "Quick Tournament" : type).adaptToDiscordChat();
                                    newDef1.Inline = true;
                                    deflist.Add(newDef1);
                                    DEF newDef2 = new DEF();
                                    newDef2.Name = "Wanneer?";
                                    newDef2.Value = wanneer.adaptToDiscordChat();
                                    newDef2.Inline = true;
                                    deflist.Add(newDef2);
                                    DEF newDef3 = new DEF();
                                    newDef3.Name = "Organisator";
                                    newDef3.Value = ctx.Member.DisplayName.adaptToDiscordChat();
                                    newDef3.Inline = true;
                                    deflist.Add(newDef3);

                                    List<DiscordEmoji> emojiList = new List<DiscordEmoji>();
                                    for (int i = 0; i < tiers_gesplitst_met_spatie.Length; i++)
                                    {
                                        emojiList.Add(Bot.GetDiscordEmoji(Emoj.getName(Convert.ToInt32(tiers_gesplitst_met_spatie[i]))));
                                    }

                                    await Bot.CreateEmbed(toernooiAanmeldenChannel, string.Empty, "@everyone", "Toernooi", string.Empty, deflist, emojiList, string.Empty, null);
                                }
                                else
                                {
                                    await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Het kanaal #Toernooi-aanmelden kon niet gevonden worden!**");
                                }
                            }
                            else
                            {
                                await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**De tiers moeten groter dan 0 en maximum 10 zijn!**");
                            }
                        }
                        else
                        {
                            await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Je moet als tiers getallen opgeven!**");
                        }
                    }
                    else
                    {
                        await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Je moet minstens één tier geven!**");
                    }
                    await Bot.ConfirmCommandExecuted(ctx.Message);
                }
                else
                {
                    await Bot.SayTheUserIsNotAllowed(ctx.Channel);
                }
            }
        }
        #region tagteams
        [Command("Tagteams")]
        [Aliases("tt", "tagt", "tagte", "tagtea", "tgt", "tgte", "tgtea", "tgteam", "tte", "ttea", "tteam")]
        [Description("Tagt alle gebruikers die zich voor het bepaalde toernooi aangemeld hebben.\n" +
            "Voer deze commando uit in het kanaal waar het bericht geplaatst moet worden. " +
            "De bot zal dan je commando verwijderen en zelf een bericht plaatsen met dezelfde inhoud en tagt de mensen die zich aangemeld hebben voor het toernooi.")]
        public async Task TagTeams(CommandContext ctx, params string[] optioneel_wat_je_wilt_zeggen)
        {
            if (!Bot.ignoreCommands)
            {
                if (Bot.HasRight(ctx.Member, ctx.Command))
                {
                    await Bot.ConfirmCommandExecuting(ctx.Message);
                    //remove message
                    await ctx.Channel.DeleteMessageAsync(ctx.Message);

                    //execute rest of command
                    List<Tier> tiers = await Bot.ReadTeams(ctx.Channel, ctx.Member, ctx.Guild.Name, new string[] { "1" });
                    if (tiers != null)
                    {
                        List<Tuple<ulong, string>> uniqueMemberList = await Bot.GetIndividualParticipants(tiers, ctx.Guild);
                        List<string> mentionList = await Bot.GetMentions(uniqueMemberList, ctx.Guild.Id);
                        if (mentionList != null)
                        {
                            if (mentionList.Count > 0)
                            {
                                StringBuilder sb = new StringBuilder("**");
                                bool firstTime = true;
                                foreach (string gebruiker in mentionList)
                                {
                                    if (firstTime)
                                    {
                                        firstTime = false;
                                    }
                                    else
                                    {
                                        sb.Append(' ');
                                    }
                                    sb.Append(gebruiker);
                                }
                                sb.Append("**");
                                if (optioneel_wat_je_wilt_zeggen.Length > 0)
                                {
                                    StringBuilder sbTekst = new StringBuilder();
                                    firstTime = true;
                                    foreach (string word in optioneel_wat_je_wilt_zeggen)
                                    {
                                        if (firstTime)
                                        {
                                            firstTime = false;
                                        }
                                        else
                                        {
                                            sbTekst.Append(' ');
                                        }
                                        sbTekst.Append(word);
                                    }
                                    sb.Append("\n\n");
                                    sb.Append(sbTekst.ToString());
                                }
                                await ctx.Channel.SendMessageAsync(sb.ToString());
                            }
                            else
                            {
                                await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Er konden geen mentions geladen worden.**");
                            }
                        }
                        else
                        {
                            await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**De mentions konden niet geladen worden.**");
                        }
                        //zet individuele spelers in lijst --> pas bot.getindividual... aan naar Tuple<ulong, string>
                        //Maak een list van tags (op basis van ID, maar indien het niet gaad gewoon letterlijk #item2
                    }
                    else
                    {
                        await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**De teams konden niet geladen worden.**");
                    }
                    //await Bot.confirmCommandExecuted(ctx.Message);
                }
                else
                {
                    await Bot.SayTheUserIsNotAllowed(ctx.Channel);
                }
            }
        }
        //[Command("Tagteams")]
        //[Aliases("tt", "tagt", "tagte", "tagtea", "tgt", "tgte", "tgtea", "tgteam", "tte", "ttea", "tteam")]
        //[Description("Tagt alle gebruikers die zich voor het bepaalde toernooi aangemeld hebben.")]
        //public async Task TagTeams(CommandContext ctx, string hoeveelste_bericht, string kanaal, params string[] optioneel_wat_je_wilt_zeggen)
        //{
        //    if (!Bot.ignoreCommands)
        //    {
        //        if (Bot.hasRight(ctx.Member, ctx.Command))
        //        {
        //            await Bot.confirmCommandExecuting(ctx.Message);
        //            //zoek het juiste kanaal
        //            DiscordChannel destChannel = await Bot.getChannelBasedOnString(kanaal, ctx.Guild.Id);
        //            if (destChannel != null)
        //            {
        //                string[] tempStringArray = new string[1];
        //                tempStringArray[0] = hoeveelste_bericht;
        //                List<Tier> tiers = await Bot.readTeams(ctx.Channel, ctx.Member, ctx.Guild.Name, tempStringArray);
        //                if (tiers != null)
        //                {
        //                    List<Tuple<ulong, string>> uniqueMemberList = await Bot.getIndividualParticipants(tiers, ctx.Guild);
        //                    List<string> mentionList = await Bot.getMentions(uniqueMemberList, ctx.Guild.Id);
        //                    if (mentionList != null)
        //                    {
        //                        if (mentionList.Count > 0)
        //                        {
        //                            StringBuilder sb = new StringBuilder("**");
        //                            bool firstTime = true;
        //                            foreach (string gebruiker in mentionList)
        //                            {
        //                                if (firstTime)
        //                                {
        //                                    firstTime = false;
        //                                }
        //                                else
        //                                {
        //                                    sb.Append(' ');
        //                                }
        //                                sb.Append(gebruiker);
        //                            }
        //                            sb.Append("**");
        //                            if (optioneel_wat_je_wilt_zeggen.Length > 0)
        //                            {
        //                                StringBuilder sbTekst = new StringBuilder();
        //                                firstTime = true;
        //                                foreach (string word in optioneel_wat_je_wilt_zeggen)
        //                                {
        //                                    if (firstTime)
        //                                    {
        //                                        firstTime = false;
        //                                    }
        //                                    else
        //                                    {
        //                                        sbTekst.Append(' ');
        //                                    }
        //                                    sbTekst.Append(word);
        //                                }
        //                                sb.Append("\n\n");
        //                                sb.Append(sbTekst.ToString());
        //                            }
        //                            await destChannel.SendMessageAsync(sb.ToString());
        //                        }
        //                        else
        //                        {
        //                            await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Er konden geen mentions geladen worden.**");
        //                        }
        //                    }
        //                    else
        //                    {
        //                        await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**De mentions konden niet geladen worden.**");
        //                    }
        //                    //zet individuele spelers in lijst --> pas bot.getindividual... aan naar Tuple<ulong, string>
        //                    //Maak een list van tags (op basis van ID, maar indien het niet gaad gewoon letterlijk #item2
        //                }
        //                else
        //                {
        //                    await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**De teams konden niet geladen worden.**");
        //                }
        //            }
        //            else
        //            {
        //                await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Kanaal(" + kanaal + ") kon niet gevonden worden!**");
        //            }
        //            await Bot.confirmCommandExecuted(ctx.Message);
        //        }
        //        else
        //        {
        //            await Bot.SayTheUserIsNotAllowed(ctx.Channel);
        //        }
        //    }
        //}
#endregion
        [Command("Teams")]
        [Aliases("te", "tea", "team")]
        [Description("Geeft de teams voor het gegeven toernooi." +
            "Bijvoorbeeld:`" + Bot.Prefix + "teams` --> geeft de teams van het meest recente bericht in Toernooi-aanmelden\n`" + Bot.Prefix + "teams 1` --> geeft de teams van het meest recente bericht in Toernooi-aanmelden\n`" + Bot.Prefix + "teams 2` --> geeft de teams van het 2de meest recente bericht in Toernooi-aanmelden")]
        public async Task Teams(CommandContext ctx, params string[] optioneel_hoeveelste_toernooi_startende_vanaf_1_wat_de_recentste_voorstelt)
        {
            if (!Bot.ignoreCommands)
            {
                if (Bot.HasRight(ctx.Member, ctx.Command))
                {
                    await Bot.ConfirmCommandExecuting(ctx.Message);
                    List<Tier> tiers = await Bot.ReadTeams(ctx.Channel, ctx.Member, ctx.Guild.Name, optioneel_hoeveelste_toernooi_startende_vanaf_1_wat_de_recentste_voorstelt);
                    if (tiers != null && tiers.Count > 0)
                    {
                        List<DEF> deflist = new List<DEF>();
                        foreach (Tier aTier in tiers)
                        {
                            DEF def = new DEF();
                            def.Inline = true;
                            def.Name = "Tier " + aTier.TierNummer;
                            int counter = 1;
                            StringBuilder sb = new StringBuilder();
                            foreach (Tuple<ulong, string> user in aTier.Deelnemers)
                            {
                                string tempName = string.Empty;
                                if (aTier.isEditedWithRedundance())
                                {
                                    tempName = user.Item2;
                                }
                                else
                                {
                                    try
                                    {
                                        DiscordMember tempMember = await ctx.Guild.GetMemberAsync(user.Item1);
                                        if (tempName != null)
                                        {
                                            if (tempMember.DisplayName != null)
                                            {
                                                if (tempMember.DisplayName.Length > 0)
                                                {
                                                    tempName = tempMember.DisplayName;
                                                }
                                            }
                                        }
                                    }
                                    catch
                                    {

                                    }
                                    if (tempName.Equals(string.Empty))
                                    {
                                        tempName = user.Item2;
                                    }
                                }
                                sb.AppendLine(counter + ". " + tempName);
                                counter++;
                            }
                            def.Value = sb.ToString();
                            deflist.Add(def);
                        }
                        List<Tuple<ulong, string>> tempParticipants = await Bot.GetIndividualParticipants(tiers, ctx.Guild);
                        List<Tuple<ulong, string>> participants = Bot.RemoveSyntaxes(tempParticipants);
                        if (tiers.Count > 1)
                        {
                            participants.Sort();
                            participants.Reverse();
                            StringBuilder sb = new StringBuilder();
                            foreach (Tuple<ulong, string> participant in participants)
                            {
                                sb.AppendLine(participant.Item2);
                            }
                            DEF newDef = new DEF();
                            newDef.Inline = true;
                            newDef.Name = "Alle deelnemers (" + participants.Count + "):";
                            newDef.Value = sb.ToString().adaptToDiscordChat();
                            deflist.Add(newDef);
                        }

                        await Bot.CreateEmbed(ctx.Channel, string.Empty, string.Empty, "Teams", (tiers.Count > 0 ? "Organisator: " + tiers[0].Organisator : "Geen teams"), deflist, null, string.Empty, null);
                    }
                    else if (tiers == null)
                    {
                        await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**De teams konden niet geladen worden.**");
                    }
                    await Bot.ConfirmCommandExecuted(ctx.Message);
                }
                else
                {
                    await Bot.SayTheUserIsNotAllowed(ctx.Channel);
                }
            }
        }
        [Command("Poll")]
        [Aliases("p", "po", "pol")]
        [Description("Creëert een nieuwe poll." +
            "Bijvoorbeeld:`" + Bot.Prefix + "poll \"Een titel tussen aanhalingstekens indien er spaties zijn\" Vlaanderen :one: Wallonië :two:`\n`" + Bot.Prefix + "poll test de hemel :thumbsup: de hemel, de hel :thinking: de hel :thumbsdown:`")]
        public async Task Poll(CommandContext ctx, string uitleg, params string[] opties_gesplitst_met_emoji_als_laatste_en_mag_met_spaties)
        {
            if (!Bot.ignoreCommands)
            {
                if (Bot.HasRight(ctx.Member, ctx.Command))
                {
                    await Bot.ConfirmCommandExecuting(ctx.Message);
                    DiscordChannel pollChannel = await Bot.GetPollsChannel(false, ctx.Guild.Id);
                    if (pollChannel != null)
                    {
                        List<DEF> deflist = new List<DEF>();
                        Dictionary<string, DiscordEmoji> theList = new Dictionary<string, DiscordEmoji>();
                        List<DiscordEmoji> emojiList = new List<DiscordEmoji>();
                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < opties_gesplitst_met_emoji_als_laatste_en_mag_met_spaties.Length; i++)
                        {
                            bool isEmoji = false;
                            DiscordEmoji emoji = null;
                            try
                            {
                                emoji = Bot.GetDiscordEmoji(opties_gesplitst_met_emoji_als_laatste_en_mag_met_spaties[i]);
                                string temp = emoji.GetDiscordName();
                                DiscordEmoji tempEmoji = DiscordEmoji.FromName(Bot.discordClient, temp);
                                isEmoji = true;
                            }
                            catch
                            {

                            }
                            if (isEmoji)
                            {
                                theList.Add(sb.ToString(), emoji);
                                emojiList.Add(emoji);
                                sb.Clear();
                            }
                            else
                            {
                                if (sb.Length > 0)
                                {
                                    sb.Append(' ');
                                }
                                sb.Append(opties_gesplitst_met_emoji_als_laatste_en_mag_met_spaties[i]);
                            }
                        }
                        foreach (KeyValuePair<string, DiscordEmoji> item in theList)
                        {
                            DEF def = new DEF();
                            def.Inline = true;
                            def.Name = item.Key.adaptToDiscordChat();
                            def.Value = item.Value;
                            deflist.Add(def);
                        }

                        await Bot.CreateEmbed(pollChannel, string.Empty, string.Empty, "Poll", uitleg.adaptToDiscordChat(), deflist, emojiList, string.Empty, null);
                    }
                    else
                    {
                        await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Het kanaal #polls kon niet gevonden worden!**");
                    }
                    await Bot.ConfirmCommandExecuted(ctx.Message);
                }
                else
                {
                    await Bot.SayTheUserIsNotAllowed(ctx.Channel);
                }
            }
        }
        [Command("Deputypoll")]
        [Aliases("dp", "dpo", "dpol", "dpoll", "depoll", "deppoll", "depupoll", "deputpoll")]
        [Description("Creëert een nieuwe poll ivm de kandidaat/inactieve clanleden.\n\n" +
            "De mogelijke Tags zijn:\n`nlbe`\n`nlbe2`\n`all` (= algemene deputies rol)\n\n" +
            "De mogelijke ondewerwerpen zijn:\n`nieuw` (= indien er een nieuw kandidaat-clanclid is)\n`inactief` (= indien een clanclid inactief is)\n`overstap` (= indien clanlid van NLBE2 naar NLBE wilt overstappen)\n\n" +
            "De mogelijke reacties zijn:\n:thumbsup: = akkoord\n:thinking: = neutraal\n:thumbsdown: = Niet akkoord\n\n" +
            "Indien je opnieuw een naam mee geeft dan kan je kiezen uit:\n`ja`\n`stop`\nindien iets anders herhaalt hij het gewoon\n\nIndien je niet antwoord binnen de 30s dan stopt de bot gewoon met vragen en stopt hij ook met de commando verder uit te voeren.")]
        public async Task deputypoll(CommandContext ctx, string Tag, string Onderwerp, string speler_naam, params string[] optioneel_clan_naam_indien_nieuwe_kandidaat)
        {
            // 3 reacties voorzien, :thumbsup: :thinking: :thumbsdown:
            if (!Bot.ignoreCommands)
            {
                if (Bot.HasRight(ctx.Member, ctx.Command))
                {
                    await Bot.ConfirmCommandExecuting(ctx.Message);
                    bool validChannel = false;
                    DiscordChannel deputiesChannel = await Bot.GetDeputiesChannel();
                    if (deputiesChannel != null && ctx.Channel.Id.Equals(deputiesChannel.Id))
                    {
                        validChannel = true;
                    }
                    if (!validChannel)
                    {
                        DiscordChannel bottestChannel = await Bot.GetBottestChannel();
                        if (bottestChannel != null && ctx.Channel.Id.Equals(bottestChannel.Id))
                        {
                            validChannel = true;
                        }
                    }
                    if (!validChannel)
                    {
                        DiscordChannel bottestChannel = await Bot.GetTestChannel();
                        if (bottestChannel != null && ctx.Channel.Id.Equals(bottestChannel.Id))
                        {
                            validChannel = true;
                        }
                    }
                    if (validChannel)
                    {
                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < optioneel_clan_naam_indien_nieuwe_kandidaat.Length; i++)
                        {
                            if (i > 0)
                            {
                                sb.Append(' ');
                            }
                            sb.Append(optioneel_clan_naam_indien_nieuwe_kandidaat[i]);
                        }
                        DiscordChannel deputiesPollsChannel = await Bot.GetPollsChannel(true, ctx.Guild.Id);
                        //https://www.blitzstars.com/player/eu/
                        bool goodOption = true;
                        DiscordRole deputiesNLBERole = ctx.Guild.GetRole(Bot.DEPUTY_NLBE_ROLE);
                        DiscordRole deputiesNLBE2Role = ctx.Guild.GetRole(Bot.DEPUTY_NLBE2_ROLE);
                        switch (Tag.ToLower())
                        {
                            case "nlbe":
                                if (deputiesNLBERole != null)
                                {
                                    Tag = deputiesNLBERole.Mention;
                                }
                                else
                                {
                                    Tag = "@Deputy-NLBE";
                                }
                                break;
                            case "nlbe2":
                                if (deputiesNLBE2Role != null)
                                {
                                    Tag = deputiesNLBE2Role.Mention;
                                }
                                else
                                {
                                    Tag = "@Deputy-NLBE2";
                                }
                                break;
                            case "all":
                                DiscordRole deputiesRole = ctx.Guild.GetRole(Bot.DEPUTY_ROLE);
                                if (deputiesRole != null)
                                {
                                    Tag = deputiesRole.Mention;
                                }
                                else if (deputiesNLBERole != null && deputiesNLBE2Role != null)
                                {
                                    Tag = deputiesNLBERole.Mention + " " + deputiesNLBE2Role.Mention;
                                }
                                else
                                {
                                    Tag = "@Deputy";
                                }
                                break;
                            default: goodOption = false; break;
                        }
                        if (goodOption)
                        {
                            string originalWat = Onderwerp;
                            switch (Onderwerp.ToLower())
                            {
                                case "nieuw": Onderwerp = "Er heeft zich een nieuwe kandidaat voor <clan> gemeld, <|>. Dit zijn zijn stats:\n<link>.\n\nGraag hieronder stemmen."; break;
                                case "inactief": Onderwerp = "<|><clan> heeft zijn laatste battle gespeeld op <dd-mm-jjjj> en heeft de laatste 90 dagen **<90>** battles gespeeld.\nDeze speler sloot zich op <dd-mm-yyyy> aan in de clan.\nZullen we afscheid van hem nemen?\n\nGraag hieronder stemmen."; break;
                                case "overstap": Onderwerp = "<|> zou graag willen overstappen van NLBE2 naar NLBE.\nGaan jullie hiermee akkoord?\n\nGraag hieronder stemmen."; break;
                                default: goodOption = false; break;
                            }
                            if (goodOption)
                            {
                                bool hasAnswered = false;
                                bool hasConfirmed = false;
                                bool firstTime = true;
                                WGAccount account = new WGAccount(Bot.WarGamingAppId, 552887317, false, true, false);
                                while (!hasAnswered || !hasConfirmed)
                                {
                                    if (firstTime)
                                    {
                                        firstTime = false;
                                    }
                                    else
                                    {
                                        await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Geef opnieuw een naam:**");
                                        var interactivityx = ctx.Client.GetInteractivity();
                                        var messagex = await interactivityx.WaitForMessageAsync(x => x.Channel == ctx.Channel && x.Author == ctx.User);
                                        if (!messagex.TimedOut)
                                        {
                                            if (messagex.Result != null)
                                            {
                                                if (messagex.Result.Content != null)
                                                {
                                                    speler_naam = messagex.Result.Content;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            await Bot.SayNoResponse(ctx.Channel);
                                            break;
                                        }
                                    }
                                    account = await Bot.SearchPlayer(ctx.Channel, ctx.Member, ctx.User, ctx.Guild.Name, speler_naam);
                                    if (account != null)
                                    {
                                        await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Is dit de gebruiker dat je zocht? ( ja / nee )**");
                                        var interactivity = ctx.Client.GetInteractivity();
                                        var message = await interactivity.WaitForMessageAsync(x => x.Channel == ctx.Channel && x.Author == ctx.User);
                                        if (!message.TimedOut)
                                        {
                                            if (message.Result != null)
                                            {
                                                if (message.Result.Content != null)
                                                {
                                                    if (message.Result.Content.ToLower().Equals("ja"))
                                                    {
                                                        hasAnswered = true; hasConfirmed = true; break;
                                                    }
                                                    if (message.Result.Content.ToLower().Equals("stop"))
                                                    {
                                                        hasAnswered = true; hasConfirmed = false; break;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            await Bot.SayNoResponse(ctx.Channel);
                                            break;
                                        }
                                    }
                                }
                                if (hasAnswered && hasConfirmed)
                                {
                                    goodOption = false;
                                    if (account != null)
                                    {
                                        if (account.nickname != null)
                                        {
                                            if (account.nickname.Length > 0)
                                            {
                                                bool allGood = true;
                                                goodOption = true;
                                                var link = "www.blitzstars.com/player/eu/" + account.nickname;
                                                Onderwerp = Onderwerp.Replace("<|>", "**" + account.nickname.adaptToDiscordChat() + "**");
                                                Onderwerp = Onderwerp.Replace("<link>", "[" + link + "](https://" + link + ")");
                                                if (account.last_battle_time.HasValue)
                                                {
                                                    Onderwerp = Onderwerp.Replace("<dd-mm-jjjj>", account.last_battle_time.Value.Day + "-" + account.last_battle_time.Value.Month + "-" + account.last_battle_time.Value.Year);
                                                }
                                                if (account.clan != null && account.clan.joined_at.HasValue)
                                                {
                                                    Onderwerp = Onderwerp.Replace("<dd-mm-yyyy>", account.clan.joined_at.Value.Day + "-" + account.clan.joined_at.Value.Month + "-" + account.clan.joined_at.Value.Year);
                                                }
                                                var amountOfBattles90 = Handler.Get90DayBattles(account.account_id);
                                                Onderwerp = Onderwerp.Replace("<90>", amountOfBattles90.ToString());
                                                if (originalWat.ToLower().Equals("nieuw"))
                                                {
                                                    if (sb.Length > 0)
                                                    {
                                                        Onderwerp = Onderwerp.Replace("<clan>", "**" + sb + "**");
                                                    }
                                                    else
                                                    {
                                                        await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Je moet de clan meegeven waarin de speler wilt joinen!**");
                                                        allGood = false;
                                                    }
                                                }
                                                else
                                                {
                                                    bool clanFound = false;
                                                    if (account.clan != null)
                                                    {
                                                        if (account.clan.tag != null)
                                                        {
                                                            clanFound = true;
                                                            Onderwerp = Onderwerp.Replace("<clan>", " van **" + account.clan.tag + "**");
                                                        }
                                                    }
                                                    if (!clanFound)
                                                    {
                                                        Onderwerp = Onderwerp.Replace("<clan>", string.Empty);
                                                    }
                                                }
                                                if (allGood)
                                                {
                                                    List<DiscordEmoji> emojies = new List<DiscordEmoji>();
                                                    emojies.Add(Bot.GetDiscordEmoji(":thumbsup:"));
                                                    emojies.Add(Bot.GetDiscordEmoji(":thinking:"));
                                                    emojies.Add(Bot.GetDiscordEmoji(":thumbsdown:"));
                                                    DiscordEmbedBuilder.EmbedAuthor author = new DiscordEmbedBuilder.EmbedAuthor();
                                                    author.Name = ctx.Member.DisplayName;
                                                    author.IconUrl = ctx.Member.AvatarUrl;
                                                    await Bot.CreateEmbed(deputiesPollsChannel, string.Empty, Tag, "Poll", Onderwerp, null, emojies, string.Empty, author);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            goodOption = false;
                                        }
                                    }
                                    if (!goodOption)
                                    {
                                        await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Kon de speler niet vinden.**");
                                    }
                                }
                            }
                            else
                            {
                                await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Je hebt een verkeerde `Wat` meegegeven.**");
                            }
                        }
                        else
                        {
                            await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Je hebt een verkeerde `Tag` meegegeven.**");
                        }
                    }
                    else
                    {
                        await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Je mag deze commando enkel vanuit " + (deputiesChannel != null ? deputiesChannel.Mention : "#deputies") + " uitvoeren!**");
                    }
                    await Bot.ConfirmCommandExecuted(ctx.Message);
                }
                else
                {
                    await Bot.SayTheUserIsNotAllowed(ctx.Channel);
                }
            }
        }
        [Command("Map")]
        [Aliases("m", "ma", "maps")]
        [Description("Laadt de map in de chat." +
            "Bijvoorbeeld:`" + Bot.Prefix + "map` --> geeft de lijst van mappen\n`" + Bot.Prefix + "map list` --> geeft de lijst van mappen\n`" + Bot.Prefix + "map mines` --> geeft de map \"Mines\"")]
        public async Task MapLoader(CommandContext ctx, params string[] map)
        {
            if (!Bot.ignoreCommands)
            {
                if (Bot.HasRight(ctx.Member, ctx.Command))
                {
                    await Bot.ConfirmCommandExecuting(ctx.Message);
                    List<Tuple<string, string>> images = await Bot.GetAllMaps(ctx.Guild.Id);
                    if (images != null)
                    {
                        StringBuilder sbMap = new StringBuilder();
                        for (int i = 0; i < map.Count(); i++)
                        {
                            if (i > 0)
                            {
                                sbMap.Append(' ');
                            }
                            sbMap.Append(map[i]);
                        }
                        if (sbMap.ToString().ToLower().Equals("list") || sbMap.Length == 0)
                        {
                            StringBuilder sb = new StringBuilder();
                            foreach (Tuple<string, string> item in images)
                            {
                                sb.AppendLine(item.Item1);
                            }
                            await Bot.CreateEmbed(ctx.Channel, string.Empty, string.Empty, "Mappen", sb.ToString(), null, null, string.Empty, null);
                        }
                        else
                        {
                            bool mapFound = false;
                            foreach (Tuple<string, string> item in images)
                            {
                                if (item.Item1.ToLower().Contains(sbMap.ToString().ToLower()))
                                {
                                    mapFound = true;
                                    await Bot.CreateEmbed(ctx.Channel, string.Empty, string.Empty, item.Item1, string.Empty, null, null, item.Item2, null);
                                    break;
                                }
                            }
                            if (!mapFound)
                            {
                                await Bot.CreateEmbed(ctx.Channel, string.Empty, string.Empty, "De map `" + sbMap.ToString() + "` kon niet gevonden worden.", string.Empty, null, null, string.Empty, null);
                            }
                        }
                        #region backup
                        //if (map.Length > 0)
                        //{
                        //    if (map[0].ToLower() == "list" && map.Length == 1)
                        //    {
                        //        StringBuilder sb = new StringBuilder();
                        //        List<string> mappen = new List<string>();
                        //        mappen.AddRange(images.Keys);
                        //        mappen.Sort();
                        //        for (int i = 0; i < mappen.Count; i++)
                        //        {
                        //            sb.AppendLine(mappen[i]);
                        //        }

                        //        await Bot.CreateEmbed(ctx.Channel, string.Empty, string.Empty, "Mappen", sb.ToString(), null, null, string.Empty, null);
                        //    }
                        //    else
                        //    {
                        //        bool neverFound = true;
                        //        for (int i = 0; i < images.Count; i++)
                        //        {
                        //            bool containsAll = true;
                        //            for (int j = 0; j < map.Length; j++)
                        //            {
                        //                if (!images.ContainsKey(map[j].ToLower()))
                        //                {
                        //                    containsAll = false;
                        //                    break;
                        //                }
                        //            }
                        //            if (containsAll && map.Length > 0)
                        //            {
                        //                string temp = string.Empty;
                        //                neverFound = false;
                        //                foreach(KeyValuePair<string, string> items in images)
                        //                {

                        //                }
                        //                await Bot.CreateEmbed(ctx.Channel, string.Empty, string.Empty, temp, string.Empty, null, null, images[i], null);
                        //                break;
                        //            }
                        //            else if ((i + 1) == images.Count && neverFound)
                        //            {
                        //                StringBuilder sb = new StringBuilder();
                        //                sb.Append('`');
                        //                for (int j = 0; j < map.Length; j++)
                        //                {
                        //                    if (j > 0)
                        //                    {
                        //                        sb.Append(' ');
                        //                    }
                        //                    sb.Append(map[j]);
                        //                }
                        //                sb.Append('`');
                        //                await Bot.CreateEmbed(ctx.Channel, string.Empty, string.Empty, "De map " + sb.ToString() + " kon niet gevonden worden.", string.Empty, null, null, string.Empty, null);
                        //            }
                        //        }
                        //    }
                        //}
                        //else
                        //{
                        //    StringBuilder sb = new StringBuilder();
                        //    List<string> mappen = new List<string>();
                        //    for (int i = 0; i < images.Count; i++)
                        //    {
                        //        mappen.Add(Bot.getProperFileName(images[i]));
                        //    }
                        //    mappen.Sort();
                        //    for (int i = 0; i < mappen.Count; i++)
                        //    {
                        //        sb.AppendLine(mappen[i]);
                        //    }



                        //    await Bot.CreateEmbed(ctx.Channel, string.Empty, string.Empty, "Mappen", sb.ToString(), null, null, string.Empty, null);
                        //}
                        #endregion
                    }
                    else
                    {
                        await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Kon de mappen niet uit een kanaal halen.**");
                    }
                    await Bot.ConfirmCommandExecuted(ctx.Message);
                }
                else
                {
                    await Bot.SayTheUserIsNotAllowed(ctx.Channel);
                }
            }
        }
        [Command("Reageer")]
        [Aliases("r", "re", "rea", "reag", "reage", "reagee")]
        [Description("Geeft een reactie op het gegeven bericht in het gegeven kanaal met de gegeven emoji." +
            "Bijvoorbeeld:`" + Bot.Prefix + "reageer toernooi-aanmelden 1 :two:`--> zorgt ervoor dat de bot in toernooi-aanmelden bij het meest recente bericht de emoji :two: zet\n`" + Bot.Prefix + "reageer polls 4 :tada:` --> zorgt ervoor dat de bot in polls bij het 4de meest recente bericht de emoji :tada: zet")]
        public async Task react(CommandContext ctx, string naam_van_kanaal, int hoeveelste_bericht, string emoji)
        {
            if (!Bot.ignoreCommands)
            {
                if (Bot.HasRight(ctx.Member, ctx.Command))
                {
                    await Bot.ConfirmCommandExecuting(ctx.Message);
                    var channels = ctx.Guild.Channels.Values;
                    foreach (var channel in channels)
                    {
                        if (channel.Name.Equals(naam_van_kanaal))
                        {
                            var xMessages = channel.GetMessagesAsync(hoeveelste_bericht).Result;
                            for (int i = 0; i < xMessages.Count; i++)
                            {
                                if (i == (hoeveelste_bericht - 1))
                                {
                                    DiscordEmoji theEmoji = Bot.GetDiscordEmoji(emoji);
                                    string temp = theEmoji.GetDiscordName();
                                    bool isEmoji = false;
                                    try
                                    {
                                        DiscordEmoji tempEmoji = DiscordEmoji.FromName(Bot.discordClient, temp);
                                        isEmoji = true;
                                    }
                                    catch
                                    {
                                    }
                                    if (isEmoji)
                                    {
                                        try
                                        {
                                            await xMessages[i].CreateReactionAsync(theEmoji);
                                            await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Reactie(" + emoji + ") van bericht(" + hoeveelste_bericht + ") in kanaal(" + naam_van_kanaal + ") is toegevoegd!**");
                                        }
                                        catch (Exception ex)
                                        {
                                            await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Kon geen reactie(" + emoji + ") toevoegen bij bericht(" + hoeveelste_bericht + ") in kanaal(" + naam_van_kanaal + ")!**");
                                            Bot.discordClient.Logger.LogWarning("Could not add reaction(" + emoji + ") for message(" + hoeveelste_bericht + ") in channel(" + naam_van_kanaal + "):" + ex.Message);
                                        }
                                    }
                                    else
                                    {
                                        await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**De emoji(" + emoji + ") geen bestaande emoji!**");
                                    }
                                }
                            }
                        }
                    }
                    await Bot.ConfirmCommandExecuted(ctx.Message);
                }
                else
                {
                    await Bot.SayTheUserIsNotAllowed(ctx.Channel);
                }
            }
        }
        [Command("Verwijderreactie")]
        [Aliases("vr", "v", "ve", "ver", "verw", "verwi", "verwij", "verwijd", "verwijde", "verwijder", "verwijderr", "verwijderre", "verwijderrea", "verwijderreac", "verwijderreact", "verwijderreacti", "verwijdereactie")]
        [Description("Verwijdert een reactie van het gegeven bericht in het gegeven kanaal met de gegeven emoji." +
            "Bijvoorbeeld:`" + Bot.Prefix + "verwijderreactie toernooi-aanmelden 1 :two:`--> zorgt ervoor dat de bot in toernooi-aanmelden bij het meest recente bericht de emoji :two: verwijdert\n`" + Bot.Prefix + "verwijderreactie polls 4 :tada:` --> zorgt ervoor dat de bot in polls bij het 4de meest recente bericht de emoji :tada: verwijdert")]
        public async Task removeReact(CommandContext ctx, string naam_van_kanaal, string hoeveelste_bericht, string emoji)
        {
            if (!Bot.ignoreCommands)
            {
                if (Bot.HasRight(ctx.Member, ctx.Command))
                {
                    await Bot.ConfirmCommandExecuting(ctx.Message);
                    int hoeveelste = -1;
                    bool goodNumber = true;
                    try
                    {
                        hoeveelste = Convert.ToInt32(hoeveelste_bericht);
                    }
                    catch
                    {
                        goodNumber = false;
                    }
                    if (hoeveelste > 0)
                    {
                        hoeveelste--;
                        var channels = ctx.Guild.Channels.Values;
                        bool channelFound = false;
                        foreach (var channel in channels)
                        {
                            if (channel.Name.Equals(naam_van_kanaal))
                            {
                                channelFound = true;
                                var zMessages = channel.GetMessagesAsync(hoeveelste + 1).Result;
                                IReadOnlyList<DiscordUser> userReactionsFromTheEmoji = new List<DiscordUser>();
                                DiscordEmoji theEmoji = Bot.GetDiscordEmoji(emoji);
                                string temp = theEmoji.GetDiscordName();
                                bool isEmoji = false;
                                try
                                {
                                    DiscordEmoji tempEmoji = DiscordEmoji.FromName(Bot.discordClient, temp);
                                    isEmoji = true;
                                }
                                catch
                                {
                                }
                                if (isEmoji)
                                {
                                    try
                                    {
                                        userReactionsFromTheEmoji = await zMessages[hoeveelste].GetReactionsAsync(theEmoji);
                                        await zMessages[hoeveelste].DeleteReactionsEmojiAsync(theEmoji);
                                        await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Reactie(" + emoji + ") van bericht(" + (hoeveelste + 1) + ") in kanaal(" + naam_van_kanaal + ") is verwijderd!**");
                                    }
                                    catch (Exception ex)
                                    {
                                        await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Kon reactie(" + emoji + ") van bericht(" + (hoeveelste + 1) + ") in kanaal(" + naam_van_kanaal + ") niet verwijderen!**");
                                        Bot.discordClient.Logger.LogWarning("Could not remove reaction(" + emoji + ") from message(" + (hoeveelste + 1) + ") in channel(" + naam_van_kanaal + "):" + ex.Message);
                                    }
                                    if (channel.Id.Equals(Bot.NLBE_TOERNOOI_AANMELDEN_KANAAL_ID) || channel.Id.Equals(Bot.DA_BOIS_TOERNOOI_AANMELDEN_KANAAL_ID))
                                    {
                                        List<DiscordMessage> messages = new List<DiscordMessage>();
                                        try
                                        {
                                            var xMessages = channel.GetMessagesAsync(hoeveelste + 1).Result;
                                            foreach (var message in xMessages)
                                            {
                                                messages.Add(message);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            await Bot.HandleError("Could not load messages from " + channel.Name + ": ", ex.Message, ex.StackTrace);
                                        }
                                        if (messages.Count == (hoeveelste + 1))
                                        {
                                            DiscordMessage theMessage = messages[hoeveelste];
                                            if (theMessage != null)
                                            {
                                                if (theMessage.Author.Id.Equals(Bot.NLBE_BOT) || theMessage.Author.Id.Equals(Bot.TESTBEASTV2_BOT))
                                                {
                                                    DiscordChannel logChannel = await Bot.GetLogChannel(ctx.Guild.Id);
                                                    if (logChannel != null)
                                                    {
                                                        var logMessages = await logChannel.GetMessagesAsync(100);
                                                        Dictionary<DateTime, List<DiscordMessage>> sortedMessages = Bot.SortMessages(logMessages);
                                                        foreach (KeyValuePair<DateTime, List<DiscordMessage>> sMessage in sortedMessages)
                                                        {
                                                            string xdate = Bot.ConvertToDate(theMessage.Timestamp);
                                                            string ydate = Bot.ConvertToDate(sMessage.Key);
                                                            if (xdate.Equals(ydate))
                                                            {
                                                                List<DiscordMessage> messagesToDelete = new List<DiscordMessage>();
                                                                sMessage.Value.Sort((x, y) => x.Timestamp.CompareTo(y.Timestamp));
                                                                foreach (DiscordMessage discMessage in sMessage.Value)
                                                                {
                                                                    string[] splitted = discMessage.Content.Split(Bot.LOG_SPLIT_CHAR);
                                                                    if (splitted[1].ToLower().Equals("teams"))
                                                                    {
                                                                        //splitted[2] = naam speler
                                                                        foreach (DiscordUser user in userReactionsFromTheEmoji)
                                                                        {
                                                                            DiscordMember tempMemberByUser = await ctx.Guild.GetMemberAsync(user.Id);
                                                                            if (tempMemberByUser != null)
                                                                            {
                                                                                if (tempMemberByUser.DisplayName.Equals(splitted[2]))
                                                                                {
                                                                                    if (Bot.GetEmojiAsString(theEmoji).Equals(Bot.GetEmojiAsString(splitted[3])))
                                                                                    {
                                                                                        messagesToDelete.Add(discMessage);
                                                                                    }
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                                foreach (DiscordMessage toDeleteMessage in messagesToDelete)
                                                                {
                                                                    await toDeleteMessage.DeleteAsync();
                                                                }
                                                                if (messagesToDelete.Count > 0)
                                                                {
                                                                    await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**In de log werden er ook aanpassingen gedaan om het teams commando up-to-date te houden.**");
                                                                }
                                                                break;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        await Bot.HandleError("Could not find log channel!", string.Empty, string.Empty);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Het bericht kon niet gevonden worden!**");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Het gegeven emoji is geen bestaande emoji!**");
                                }
                                break;
                            }
                        }
                        if (!channelFound)
                        {
                            await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Kanaal kon niet gevonden worden.**");
                        }
                    }
                    else
                    {
                        if (goodNumber)
                        {
                            await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Dat getal is te klein, het moet groter dan 0 zijn!**");
                        }
                        else
                        {
                            await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Dat getal is geen bruikbaar getal!**");
                        }
                    }
                    await Bot.ConfirmCommandExecuted(ctx.Message);
                }
                else
                {
                    await Bot.SayTheUserIsNotAllowed(ctx.Channel);
                }
            }
        }
        [Command("Help")]
        [Aliases("h", "he", "hel")]
        [Description("Geeft alle commando's of geeft uitleg voor het gegeven commando." +
            "Bijvoorbeeld:`" + Bot.Prefix + "help`\n`" + Bot.Prefix + "help teams`")]
        public async Task helpCommand(CommandContext ctx, params string[] optioneel_commando)
        {
            if (!Bot.ignoreCommands)
            {
                if (Bot.HasRight(ctx.Member, ctx.Command))
                {
                    await Bot.ConfirmCommandExecuting(ctx.Message);
                    if (optioneel_commando.Length == 0)
                    {
                        StringBuilder sb = new StringBuilder();
                        var commands = ctx.CommandsNext.RegisteredCommands.Values;
                        List<string> commandoList = new List<string>();
                        foreach (var command in commands)
                        {
                            if (!commandoList.Contains(command.Name))
                            {
                                if (Bot.HasRight(ctx.Member, command))
                                {
                                    commandoList.Add(command.Name);
                                    sb.AppendLine(command.Name);
                                }
                            }
                        }
                        List<DEF> deflist = new List<DEF>();
                        DEF newDef1 = new DEF();
                        newDef1.Inline = true;
                        newDef1.Name = "Commando's";
                        newDef1.Value = sb.ToString();
                        deflist.Add(newDef1);
                        await Bot.CreateEmbed(ctx.Channel, string.Empty, string.Empty, "Help", "Versie: `" + Bot.version + "`", deflist, null, string.Empty, null);
                    }
                    else if (optioneel_commando.Length == 1)
                    {
                        var commands = ctx.CommandsNext.RegisteredCommands;
                        bool commandFound = false;
                        foreach (var command in commands)
                        {
                            if (command.Key.ToLower().Equals(optioneel_commando[0].ToLower()))
                            {
                                commandFound = true;
                                List<DEF> deflist = new List<DEF>();
                                DEF newDef1 = new DEF();
                                newDef1.Inline = true;
                                newDef1.Name = "Commando";
                                newDef1.Value = command.Value.Name;
                                deflist.Add(newDef1);
                                if (command.Value.Overloads.Count > 0)
                                {
                                    StringBuilder overloadSB = new StringBuilder();
                                    bool firstTime = true;
                                    foreach (var argument in command.Value.Overloads[0].Arguments)
                                    {
                                        if (firstTime)
                                        {
                                            firstTime = false;
                                        }
                                        else
                                        {
                                            overloadSB.Append(" ");
                                        }
                                        string argumentName = argument.Name;
                                        if (argument.Name.Contains("optioneel_"))
                                        {
                                            string[] splitted = argumentName.Split("__");
                                            argumentName = "[" + splitted[0].Replace('_', ' ').Replace("optioneel_", string.Empty) + "]";
                                            if (splitted.Length > 1)
                                            {
                                                for (int i = 1; i < splitted.Length; i++)
                                                {
                                                    argumentName += " (" + splitted[i].Replace('_', ' ') + ")";
                                                }
                                            }
                                        }
                                        else
                                        {
                                            argumentName = "(" + argumentName + ")";
                                        }
                                        overloadSB.Append(argumentName.Replace("optioneel", "OPTIONEEL").Replace('_', ' '));
                                    }
                                    if (overloadSB.Length > 0)
                                    {
                                        DEF newDef2 = new DEF();
                                        newDef2.Inline = true;
                                        newDef2.Name = "Argument" + (command.Value.Overloads[0].Arguments.Count > 1 ? "en" : "");
                                        newDef2.Value = overloadSB.ToString();
                                        deflist.Add(newDef2);
                                    }
                                }
                                if (command.Value.Aliases.Count > 0)
                                {
                                    StringBuilder aliasSB = new StringBuilder();
                                    bool firstTime = true;
                                    foreach (var alias in command.Value.Aliases)
                                    {
                                        if (firstTime)
                                        {
                                            firstTime = false;
                                        }
                                        else
                                        {
                                            aliasSB.Append(", ");
                                        }
                                        aliasSB.Append(alias);
                                    }
                                    if (aliasSB.Length > 0)
                                    {
                                        DEF newDef2 = new DEF();
                                        newDef2.Inline = true;
                                        newDef2.Name = "Alias" + (command.Value.Aliases.Count > 1 ? "sen" : "");
                                        newDef2.Value = aliasSB.ToString();
                                        deflist.Add(newDef2);
                                    }
                                }
                                if (command.Value.Description.Length > 0)
                                {
                                    DEF newDef3 = new DEF();
                                    newDef3.Inline = true;
                                    newDef3.Name = "Omschrijving";
                                    if (command.Value.Description.Contains("Bijvoorbeeld:"))
                                    {
                                        DEF newDef4 = new DEF();
                                        newDef4.Inline = true;
                                        newDef4.Name = "Bijvoorbeeld";
                                        string[] splitted = command.Value.Description.Split("Bijvoorbeeld:");
                                        newDef3.Value = splitted[0];
                                        newDef4.Value = splitted[1];
                                        deflist.Add(newDef4);
                                    }
                                    else
                                    {
                                        newDef3.Value = command.Value.Description;
                                    }
                                    deflist.Add(newDef3);
                                }
                                await Bot.CreateEmbed(ctx.Channel, string.Empty, string.Empty, "Help voor `" + command.Key + "`", string.Empty, deflist, null, string.Empty, null);
                                break;
                            }
                        }
                        if (!commandFound)
                        {
                            await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Kan info voor " + optioneel_commando[0] + " niet vinden omdat deze commando niet bestaat!**");
                        }
                    }
                    else
                    {
                        await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Te veel parameters! Max 1 parameter!**");
                    }
                    await Bot.ConfirmCommandExecuted(ctx.Message);
                }
                else
                {
                    await Bot.SayTheUserIsNotAllowed(ctx.Channel);
                }
            }
        }
        [Command("Ignore")]
        [Description("Negeert alle commando's behalve deze commando zelf tot de gebruiker dit weer inschakelt. Indien \"event\" of \"events\" als parameter meegegeven wordt, negeert hij de events. Je kan de events met dezelfde commando terug inschakelen.")]
        public async Task ignore(CommandContext ctx, params string[] optioneel_events)
        {
            if (Bot.HasRight(ctx.Member, ctx.Command))
            {
                await Bot.ConfirmCommandExecuting(ctx.Message);
                if (optioneel_events.Length > 0)
                {
                    if (!optioneel_events[0].ToLower().Contains("events") && !optioneel_events[0].ToLower().Contains("event"))
                    {
                        if (Bot.ignoreCommands)
                        {
                            Bot.ignoreCommands = false;
                        }
                        else
                        {
                            Bot.ignoreCommands = true;
                        }
                        Bot.discordClient.Logger.LogWarning(">>> NLBE-Bot negeert nu de commando's" + (Bot.ignoreCommands ? "" : " niet meer") + "! <<<");
                        await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**NLBE-Bot (`v " + Bot.version + "`) negeert nu de commando's" + (Bot.ignoreCommands ? "" : " niet meer") + "!**");
                    }
                    else
                    {
                        if (Bot.ignoreEvents)
                        {
                            Bot.ignoreEvents = false;
                        }
                        else
                        {
                            Bot.ignoreEvents = true;
                        }
                        Bot.discordClient.Logger.LogWarning(">>> NLBE-Bot negeert nu de events" + (Bot.ignoreEvents ? "" : " niet meer") + "! <<<");
                        await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**NLBE-Bot (`v " + Bot.version + "`) negeert nu de events" + (Bot.ignoreEvents ? "" : " niet meer") + "!**");
                    }
                }
                else
                {
                    if (Bot.ignoreCommands)
                    {
                        Bot.ignoreCommands = false;
                    }
                    else
                    {
                        Bot.ignoreCommands = true;
                    }
                    Bot.discordClient.Logger.LogWarning(">>> NLBE-Bot negeert nu de commando's" + (Bot.ignoreCommands ? "" : " niet meer") + "! <<<");
                    await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**NLBE-Bot (`v " + Bot.version + "`) negeert nu de commando's" + (Bot.ignoreCommands ? "" : " niet meer") + "!**");
                }
                await Bot.ConfirmCommandExecuted(ctx.Message);
            }
            else
            {
                await Bot.SayTheUserIsNotAllowed(ctx.Channel);
            }
        }
        [Command("Gebruiker")]
        [Aliases("speler", "spele", "spel", "spe", "sp", "s", "g", "ge", "geb", "gebr", "gebru", "gebrui", "gebruik", "gebruike", "gbruiker", "gbruikr", "gbrkr")]
        [Description("Geeft info over een speler.\n-i --> op ID zoeken (zoekt ook buiten de discord server)\nAnders zoekt de bot op basis van de originele gebruikersnamen van de personen in deze server." +
            "Bijvoorbeeld:`" + Bot.Prefix + "gebruiker 1`\n`" + Bot.Prefix + "gebruiker sjt`")]
        public async Task Speler(CommandContext ctx, params string[] optioneel_zoeken_op_id__zoekterm)
        {
            if (!Bot.ignoreCommands)
            {
                if (Bot.HasRight(ctx.Member, ctx.Command))
                {
                    await Bot.ConfirmCommandExecuting(ctx.Message);
                    string searchTerm = "";
                    string conditie = "";
                    List<string> temp = Bot.GetSearchTermAndCondition(optioneel_zoeken_op_id__zoekterm);
                    if (temp[0] != string.Empty)
                    {
                        searchTerm = temp[0];
                    }
                    conditie = temp[1];
                    int aantalGebruikers = 0;
                    if (searchTerm.ToLower().Contains('i'))
                    {
                        bool isInt = false;
                        ulong tempID = 0;
                        try
                        {
                            tempID = Convert.ToUInt64(conditie);
                            isInt = true;
                        }
                        catch
                        {
                            isInt = false;
                        }
                        if (isInt)
                        {
                            bool found = false;
                            bool error = false;
                            try
                            {
                                DiscordUser discordUser = await Bot.discordClient.GetUserAsync(tempID);
                                if (discordUser != null)
                                {
                                    await Bot.ShowMemberInfo(ctx.Channel, discordUser);
                                    found = true;
                                }
                            }
                            catch (Exception e)
                            {
                                error = true;
                                await Bot.HandleError("Something went wrong while showing the memberInfo:\n", e.Message, e.StackTrace);
                                await Bot.SaySomethingWentWrong(ctx.Channel, ctx.Member, ctx.Guild.Name);
                            }
                            if (!found && !error)
                            {
                                await Bot.SayNoResults(ctx.Channel, "**Gebruiker met ID `" + conditie + "` kon niet gevonden worden!**");
                            }
                        }
                        else
                        {
                            await Bot.SayMustBeNumber(ctx.Channel);
                        }
                    }
                    else
                    {
                        var members = ctx.Guild.GetAllMembersAsync().Result;
                        aantalGebruikers = members.Count;
                        List<DiscordMember> foundMemberList = new List<DiscordMember>();
                        foreach (var member in members)
                        {
                            if ((member.Username.ToLower() + "#" + member.Discriminator).Contains(conditie.ToLower()))
                            {
                                foundMemberList.Add(member);
                            }
                        }
                        if (foundMemberList.Count > 1)
                        {
                            StringBuilder sbFound = new StringBuilder();
                            for (int i = 0; i < foundMemberList.Count; i++)
                            {
                                sbFound.AppendLine((i + 1) + ". `" + foundMemberList[i].Username + "#" + foundMemberList[i].Discriminator.ToString() + "`");
                            }
                            if (sbFound.Length < 1024)
                            {
                                DiscordMessage discMessage = Bot.SayMultipleResults(ctx.Channel, sbFound.ToString());
                                var interactivity = ctx.Client.GetInteractivity();
                                var message = await interactivity.WaitForMessageAsync(x => x.Channel == ctx.Channel && x.Author == ctx.User);
                                if (!message.TimedOut)
                                {
                                    bool isInt = false;
                                    int number = -1;
                                    try
                                    {
                                        number = Convert.ToInt32(message.Result.Content);
                                        isInt = true;
                                    }
                                    catch
                                    {
                                        isInt = false;
                                    }
                                    if (isInt)
                                    {
                                        if (number > 0 && number <= foundMemberList.Count)
                                        {
                                            await Bot.ShowMemberInfo(ctx.Channel, foundMemberList[(number - 1)]);
                                        }
                                        else if (number > foundMemberList.Count)
                                        {
                                            await Bot.SayNumberTooBig(ctx.Channel);
                                        }
                                        else if (1 > number)
                                        {
                                            await Bot.SayNumberTooSmall(ctx.Channel);
                                        }
                                    }
                                    else
                                    {
                                        await Bot.SayMustBeNumber(ctx.Channel);
                                    }
                                }
                                else if (discMessage != null)
                                {
                                    List<DiscordEmoji> reacted = new List<DiscordEmoji>();
                                    for (int i = 1; i <= 10; i++)
                                    {
                                        DiscordEmoji emoji = Bot.GetDiscordEmoji(Emoj.getName(i));
                                        if (emoji != null)
                                        {
                                            var users = discMessage.GetReactionsAsync(emoji).Result;
                                            foreach (var user in users)
                                            {
                                                if (user.Id.Equals(ctx.User.Id))
                                                {
                                                    reacted.Add(emoji);
                                                }
                                            }
                                        }
                                    }

                                    if (reacted.Count == 1)
                                    {
                                        int index = Emoj.getIndex(Bot.GetEmojiAsString(reacted[0].Name));
                                        if (index > 0 && index <= foundMemberList.Count)
                                        {
                                            await Bot.ShowMemberInfo(ctx.Channel, foundMemberList[(index - 1)]);
                                        }
                                        else
                                        {
                                            await ctx.Channel.SendMessageAsync("**Dat was geen van de optionele emoji's!**");
                                        }
                                    }
                                    else if (reacted.Count > 1)
                                    {
                                        await ctx.Channel.SendMessageAsync("**Je mocht maar 1 reactie geven!**");
                                    }
                                    else
                                    {
                                        await Bot.SayNoResponse(ctx.Channel);
                                    }
                                }
                                else
                                {
                                    await Bot.SayNoResponse(ctx.Channel);
                                }
                            }
                            else
                            {
                                await Bot.SayBeMoreSpecific(ctx.Channel);
                            }
                        }
                        else if (foundMemberList.Count == 1)
                        {
                            await Bot.ShowMemberInfo(ctx.Channel, foundMemberList[0]);
                        }
                        else if (foundMemberList.Count == 0)
                        {
                            await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Gebruiker(**`" + conditie.Replace("\\", string.Empty) + "`**) kon niet gevonden worden! (In een lijst van " + aantalGebruikers + " gebruikers)**");
                        }
                    }
                    await Bot.ConfirmCommandExecuted(ctx.Message);
                }
                else
                {
                    await Bot.SayTheUserIsNotAllowed(ctx.Channel);
                }
            }
        }
        [Command("Gebruikerslijst")]
        [Aliases("gl")]
        [Description("Geeft alle members van de server als format: username#discriminator.\n-u --> username\n-d --> discriminator\n-n --> nickname\n-! --> not\n-b --> geeft bijnamen ipv standaard format (enkel voor de weergave, niet voor de filtering)\n-o --> sorteert op datum van creatie van WarGaming account (de niet gevonden accounts sorteert ie alfabetisch)\n-c --> sorteert op clanjoindatum (de niet gevonden accounts sorteert ie alfabetisch)"
        + "Bijvoorbeeld:\n`" + Bot.Prefix + "gl -n [NLBE]` --> geeft alle leden waarbij \"[NLBE]\" in de bijnaam voorkomt (die dus de NLBE rol hebben)\n" +
            "`" + Bot.Prefix + "gl -n!u [NLBE]` --> geeft de gebruikers waarbij \"[NLBE]\" niet in de gebruikersnaam voorkomt maar wel in de bijnaam\n" +
            "`" + Bot.Prefix + "gl -!n [NLBE` --> geeft alle leden waarbij \"[NLBE\" niet in de bijnaam voorkomt (dus de personen die niet in een NLBE clan zitten)\n" +
            "`" + Bot.Prefix + "gl -d 98` --> geeft alle leden waarbij de discriminator \"98\" bevat\n" +
            "`" + Bot.Prefix + "gl -nu [NLBE]` --> geeft alle leden waarvan zowel de gebruikersnaam als de bijnaam \"[NLBE]\" bevat\n" +
            "`" + Bot.Prefix + "gl -!nu [NLBE]` --> geeft de leden waarbij \"[NLBE]\" noch in de gebruikersnaam noch in de bijnaam voorkomt" +
            "`" + Bot.Prefix + "gl -on [NLBE]` --> geeft de leden waarbij \"[NLBE]\" in de bijnaam voorkomt en sorteert dit op de creatie van het WG account")]
        public async Task Spelers(CommandContext ctx, params string[] optioneel_optie_met_als_default_ud__waarde)
        {
            if (!Bot.ignoreCommands)
            {
                if (Bot.HasRight(ctx.Member, ctx.Command))
                {
                    await Bot.ConfirmCommandExecuting(ctx.Message);
                    const int COLUMNS = 3;
                    string searchTerm = "ud";
                    string conditie = "";
                    bool usersFound = false;
                    List<string> temp = Bot.GetSearchTermAndCondition(optioneel_optie_met_als_default_ud__waarde);
                    if (temp[0] != string.Empty)
                    {
                        searchTerm = temp[0];
                    }
                    conditie = temp[1];
                    IReadOnlyCollection<DiscordMember> members = ctx.Guild.GetAllMembersAsync().Result;
                    List<Tuple<StringBuilder, StringBuilder>> sbs = new List<Tuple<StringBuilder, StringBuilder>>();
                    for (int i = 0; i < COLUMNS; i++)
                    {
                        sbs.Add(new Tuple<StringBuilder, StringBuilder>(new StringBuilder(), new StringBuilder()));
                    }

                    List<DiscordMember> memberList = new List<DiscordMember>();
                    List<DiscordMember> dateNotFoundList = new List<DiscordMember>();
                    foreach (DiscordMember member in members)
                    {
                        bool goodSearchTerm = false;
                        List<bool> addList = new List<bool>();
                        if (searchTerm.ToLower().Contains('u'))
                        {
                            addList.Add(false);
                            goodSearchTerm = true;
                            if (!memberList.Contains(member))
                            {
                                bool inverted = false;
                                if (searchTerm.Contains('!'))
                                {
                                    if (searchTerm.IndexOf('!') < searchTerm.IndexOf('u'))
                                    {
                                        inverted = true;
                                    }
                                }
                                if (!inverted && member.Username.ToLower().Contains(conditie.Split('*')[0].ToLower()))
                                {
                                    addList.RemoveAt(addList.Count - 1);
                                    addList.Add(true);
                                }
                                else if (inverted && !member.Username.ToLower().Contains(conditie.Split('*')[0].ToLower()))
                                {
                                    addList.RemoveAt(addList.Count - 1);
                                    addList.Add(true);
                                }
                            }
                        }
                        if (searchTerm.ToLower().Contains('d'))
                        {
                            addList.Add(false);
                            goodSearchTerm = true;
                            if (!memberList.Contains(member))
                            {
                                bool inverted = false;
                                if (searchTerm.Contains('!'))
                                {
                                    if (searchTerm.IndexOf('!') < searchTerm.IndexOf('d'))
                                    {
                                        inverted = true;
                                    }
                                }
                                if (!inverted && member.Discriminator.ToString().Contains(conditie.Split('*')[0].ToLower()))
                                {
                                    addList.RemoveAt(addList.Count - 1);
                                    addList.Add(true);
                                }
                                else if (inverted && !member.Discriminator.ToString().Contains(conditie.Split('*')[0].ToLower()))
                                {
                                    addList.RemoveAt(addList.Count - 1);
                                    addList.Add(true);
                                }
                            }
                        }
                        if (searchTerm.ToLower().Contains('n'))
                        {
                            addList.Add(false);
                            goodSearchTerm = true;
                            if (!memberList.Contains(member))
                            {
                                bool inverted = false;
                                if (searchTerm.Contains('!'))
                                {
                                    if (searchTerm.IndexOf('!') < searchTerm.IndexOf('n'))
                                    {
                                        inverted = true;
                                    }
                                }
                                if (!inverted && member.DisplayName.ToLower().Contains(conditie.Split('*')[0].ToLower()))
                                {
                                    addList.RemoveAt(addList.Count - 1);
                                    addList.Add(true);
                                }
                                else if (inverted && !member.DisplayName.ToLower().Contains(conditie.Split('*')[0].ToLower()))
                                {
                                    addList.RemoveAt(addList.Count - 1);
                                    addList.Add(true);
                                }
                            }
                        }
                        if (searchTerm.ToLower().Contains('b'))
                        {
                            goodSearchTerm = true;
                        }
                        if (searchTerm.ToLower().Contains('o'))
                        {
                            goodSearchTerm = true;
                        }
                        if (!addList.Contains(false) && goodSearchTerm)
                        {
                            memberList.Add(member);
                        }
                        if (!goodSearchTerm)
                        {
                            sbs[0].Item1.Append("Oei!");
                            sbs[0].Item2.Append("De parameter waarop gezocht moet worden bestaat niet!");
                            break;
                        }
                    }

                    if (memberList.Count > 0)
                    {
                        usersFound = true;
                    }
                    if (searchTerm.Contains('o') && !searchTerm.Contains('c') || !searchTerm.Contains('o') && searchTerm.Contains('c'))
                    {
                        Dictionary<DateTime, DiscordMember> dateMemberList = new Dictionary<DateTime, DiscordMember>();
                        foreach (DiscordMember member in memberList)
                        {
                            string tempIGNName = string.Empty;
                            string[] splitted = member.DisplayName.Split(']');
                            if (splitted.Length > 1)
                            {
                                StringBuilder sb = new StringBuilder();
                                for (int i = 1; i < splitted.Length; i++)
                                {
                                    sb.Append(splitted[i]);
                                }
                                tempIGNName = sb.ToString().Trim();
                            }
                            else
                            {
                                tempIGNName = splitted[0].ToString().Trim();
                            }
                            //var searchResults = await Bot.wotb.Account.ListAsync(tempIGNName, WGSearchType.Exact, 20, null, null);
                            var searchResults = await WGAccount.searchByName(SearchAccuracy.EXACT, tempIGNName, Bot.WarGamingAppId, false, false, false);
                            if (searchResults != null)
                            {
                                if (searchResults.Count > 0)
                                {
                                    bool used = false;
                                    foreach (WGAccount account in searchResults)
                                    {
                                        if (account.nickname.ToLower().Equals(tempIGNName.ToLower()))
                                        {
                                            try
                                            {
                                                if (searchTerm.Contains('o'))
                                                {
                                                    if (account.created_at != null)
                                                    {
                                                        if (account.created_at.HasValue)
                                                        {
                                                            dateMemberList.Add(Bot.ConvertToDateTime(account.created_at.Value), member);
                                                            used = true;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if (account.clan != null)
                                                    {
                                                        if (account.clan.joined_at.HasValue)
                                                        {
                                                            dateMemberList.Add(Bot.ConvertToDateTime(account.clan.joined_at.Value), member);
                                                            used = true;
                                                        }
                                                    }
                                                }
                                            }
                                            catch
                                            {
                                                await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Kon gegevens niet nakijken bij **`" + account.nickname.Replace("\\", string.Empty) + "`** met als ID **`" + account.account_id + "`");
                                            }
                                        }
                                    }
                                    if (!used)
                                    {
                                        dateNotFoundList.Add(member);
                                    }
                                }
                                else
                                {
                                    dateNotFoundList.Add(member);
                                }
                            }
                            else
                            {
                                dateNotFoundList.Add(member);
                            }
                        }
                        List<KeyValuePair<DateTime, DiscordMember>> sortedDateMemberList = dateMemberList.OrderBy(p => p.Key).ToList();
                        sortedDateMemberList.Reverse();
                        memberList = new List<DiscordMember>();
                        foreach (KeyValuePair<DateTime, DiscordMember> item in sortedDateMemberList)
                        {
                            memberList.Add(item.Value);
                        }
                    }
                    else
                    {
                        if (searchTerm.Contains('b'))
                        {
                            memberList = memberList.OrderBy(p => p.DisplayName).ToList();
                        }
                        else
                        {
                            memberList = memberList.OrderBy(p => p.Username).ToList();
                        }
                    }
                    //int counter = 0;
                    //int columnCounter = 0;
                    //int rest = memberList.Count % COLUMNS;
                    //int membersPerColumn = (memberList.Count - rest) / COLUMNS;
                    int amountOfMembers = memberList.Count;
                    List<DEF> deflist = new List<DEF>();

                    if (amountOfMembers > 0)
                    {
                        deflist = Bot.ListInMemberEmbed(COLUMNS, memberList, searchTerm);
                    }

                    if (dateNotFoundList.Count > 0)
                    {
                        DEF def = new DEF();
                        def.Name = "Niet gevonden";
                        memberList = memberList.OrderBy(p => p.Username).ToList();
                        StringBuilder sb = new StringBuilder();
                        bool firstTime = true;
                        foreach (var item in dateNotFoundList)
                        {
                            if (firstTime)
                            {
                                firstTime = false;
                            }
                            else
                            {
                                sb.Append('\n');
                            }
                            sb.Append(item.DisplayName.adaptToDiscordChat());
                        }
                    }

                    string sortedBy = "alfabetisch";
                    if (searchTerm.Contains('o') && searchTerm.Contains('c'))
                    {

                    }
                    else if (searchTerm.Contains('o'))
                    {
                        sortedBy = "Creatie WG account";
                    }
                    else if (searchTerm.Contains('c'))
                    {
                        sortedBy = "Clanjoindatum";
                    }
                    await Bot.CreateEmbed(ctx.Channel, string.Empty, string.Empty, "Gebruikerslijst [" + ctx.Guild.Name.adaptToDiscordChat() + ": " + members.Count + "] (Gevonden: " + amountOfMembers + ") " + "(Gesorteerd: " + sortedBy + ")", (usersFound ? string.Empty : "Geen gebruikers gevonden die voldoen aan de zoekterm!"), (usersFound ? deflist : null), null, string.Empty, null);

                    #region select_statement_way
                    //var members = ctx.Guild.GetAllMembersAsync().Result;
                    //int aantalGebruikers = members.Count;
                    //List<List<string>> sbList = new List<List<string>>();
                    //List<StringBuilder> sbs = new List<StringBuilder>();
                    //for (int i = 0; i < 6; i++)
                    //{
                    //    sbList.Add(new List<string>());
                    //    sbs.Add(new StringBuilder());
                    //}
                    //int rowCounter = 1;
                    //foreach (var member in members)
                    //{
                    //    sbList[0].Add(rowCounter.ToString());
                    //    sbs[0].AppendLine(rowCounter.ToString());
                    //    sbList[1].Add(member.Username);
                    //    sbs[1].AppendLine(member.Username);
                    //    sbList[2].Add(member.Discriminator.ToString());
                    //    sbs[2].AppendLine(member.Discriminator.ToString());
                    //    sbList[3].Add(member.DisplayName);
                    //    sbs[3].AppendLine(member.DisplayName);
                    //    sbList[4].Add(member.Id.ToString());
                    //    sbs[4].AppendLine(member.Id.ToString());
                    //    sbList[5].Add(member.JoinedAt.Day.ToString() + "/" + member.JoinedAt.Month.ToString() + "/" + member.JoinedAt.Year.ToString());
                    //    sbs[5].AppendLine(member.JoinedAt.Day.ToString() + "/" + member.JoinedAt.Month.ToString() + "/" + member.JoinedAt.Year.ToString());
                    //    rowCounter++;
                    //}
                    //if (aantalGebruikers > 0)
                    //{
                    //    int aantalBerichten = 1;
                    //    for (int i = 0; i < sbList.Count; i++)
                    //    {
                    //        if (aantalBerichten < sbs[i].Length / 1024)
                    //        {
                    //            aantalBerichten = sbs[i].Length / 1024;
                    //        }
                    //    }
                    //    int gebruikersPerBericht = aantalGebruikers;
                    //    if (aantalBerichten > 0)
                    //    {
                    //        gebruikersPerBericht = aantalGebruikers / aantalBerichten;
                    //    }
                    //    for (int j = 0; j < aantalBerichten; j++)
                    //    {
                    //        List<DEF> deflist = new List<DEF>();
                    //        int aantalVerwijder = 0;
                    //        for (int columnCounter = 0; columnCounter < sbList.Count; columnCounter++)
                    //        {
                    //            StringBuilder sb = new StringBuilder();
                    //            for (int k = 0; k < sbList[columnCounter].Count; k++)
                    //            {
                    //                if (k < gebruikersPerBericht)
                    //                {
                    //                    sb.AppendLine(sbList[columnCounter][k]);
                    //                }
                    //                else
                    //                {
                    //                    break;
                    //                }
                    //                aantalVerwijder = k;
                    //            }
                    //            if (sb.Length > 0)
                    //            {
                    //                DEF newDef = new DEF();
                    //                newDef.Name = ".";
                    //                newDef.Inline = true;
                    //                newDef.Value = sb.ToString();
                    //                deflist.Add(newDef);
                    //            }
                    //        }
                    //        for (int k = 0; k < sbList.Count; k++)
                    //        {
                    //            for (int i = 0; i <= aantalVerwijder; i++)
                    //            {
                    //                sbList[k].RemoveAt(i);
                    //            }
                    //        }
                    //        await Bot.CreateEmbed(ctx.Channel, string.Empty, string.Empty, "Gebruikerslijst", string.Empty, deflist, null, string.Empty, null);
                    //    }
                    //}
                    //else
                    //{
                    //    await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Geen gebruikers gevonden!**");
                    //}
                    #endregion
                    await Bot.ConfirmCommandExecuted(ctx.Message);
                }
                else
                {
                    await Bot.SayTheUserIsNotAllowed(ctx.Channel);
                }
            }
        }
        [Command("Clan")]
        [Aliases("c", "cl", "cla")]
        [Description("Geeft info over de clan.")]
        public async Task clan(CommandContext ctx, string clan_naam)
        {
            if (!Bot.ignoreCommands)
            {
                if (Bot.HasRight(ctx.Member, ctx.Command))
                {
                    await Bot.ConfirmCommandExecuting(ctx.Message);
                    try
                    {
                        WGClan clan = await Bot.SearchForClan(ctx.Channel, ctx.Member, ctx.Guild.Name, clan_naam, false, ctx.User, ctx.Command);
                        if (clan != null)
                        {
                            await Bot.ShowClanInfo(ctx.Channel, clan);
                        }
                        else
                        {
                            await Bot.SayNoResults(ctx.Channel, "Geen clan gevonden met deze naam");
                        }
                    }
                    catch (TooManyResultsException e)
                    {
                        Bot.discordClient.Logger.LogWarning("(" + ctx.Command.Name + ") " + e.Message);
                        await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Te veel resultaten waren gevonden, wees specifieker!**");
                    }
                    await Bot.ConfirmCommandExecuted(ctx.Message);
                }
                else
                {
                    await Bot.SayTheUserIsNotAllowed(ctx.Channel);
                }
            }
        }
        [Command("Clanmembers")]
        [Aliases("cm", "clanm", "clanme", "clanmem", "clanmembe", "clanmember")]
        [Description("Geeft spelers van de clan.\n-s --> duid discordmembers aan\n-d --> sorteren op laatst actief")]
        public async Task clanMembers(CommandContext ctx, params string[] optioneel_discordmembers_aanduiden_en_of_sorteren_op_laatst_actief__clan_naam)
        {
            if (!Bot.ignoreCommands)
            {
                if (Bot.HasRight(ctx.Member, ctx.Command))
                {
                    await Bot.ConfirmCommandExecuting(ctx.Message);
                    string searchTerm = "";
                    string conditie = "";
                    List<string> temp = Bot.GetSearchTermAndCondition(optioneel_discordmembers_aanduiden_en_of_sorteren_op_laatst_actief__clan_naam);
                    if (temp[0] != string.Empty)
                    {
                        searchTerm = temp[0];
                    }
                    conditie = temp[1];

                    WGClan clan = await Bot.SearchForClan(ctx.Channel, ctx.Member, ctx.Guild.Name, conditie, true, ctx.User, ctx.Command);
                    if (clan != null)
                    {
                        List<Members> playersList = new List<Members>();
                        if (!searchTerm.Contains('d'))
                        {
                            playersList = clan.members.OrderBy(p => p.account_name.ToLower()).ToList();
                        }
                        else
                        {
                            playersList = clan.members;
                        }

                        List<DEF> defList = Bot.ListInPlayerEmbed(3, playersList, searchTerm, ctx.Guild);
                        string sorting = "alfabetisch";
                        if (searchTerm.Contains('d'))
                        {
                            sorting = "laatst actief";
                        }
                        await Bot.CreateEmbed(ctx.Channel, string.Empty, string.Empty, "Clanmembers van [" + clan.tag.adaptToDiscordChat() + "] (Gevonden: " + clan.members.Count + ") (Gesorteerd: " + sorting + ")", string.Empty, defList, null, string.Empty, null);
                    }
                    else
                    {
                        await Bot.SayNoResults(ctx.Channel, "Geen clan gevonden met deze naam");
                    }
                    await Bot.ConfirmCommandExecuted(ctx.Message);
                }
                else
                {
                    await Bot.SayTheUserIsNotAllowed(ctx.Channel);
                }
            }
        }
        [Command("SpelerInfo")]
        [Aliases("si")]
        [Description("Geeft wotb info van een account.\n-i --> zoekt op spelerID")]
        public async Task playerInfo(CommandContext ctx, params string[] optioneel_zoeken_op_ID__ign_naam)
        {
            if (!Bot.ignoreCommands)
            {
                if (Bot.HasRight(ctx.Member, ctx.Command))
                {
                    await Bot.ConfirmCommandExecuting(ctx.Message);
                    // -i --> zoek op ID
                    string searchTerm = "";
                    string conditie = "";
                    List<string> temp = Bot.GetSearchTermAndCondition(optioneel_zoeken_op_ID__ign_naam);
                    if (temp[0] != string.Empty)
                    {
                        searchTerm = temp[0];
                    }
                    conditie = temp[1];

                    if (searchTerm.Contains('i'))
                    {
                        long id = 0;
                        bool isLong = false;
                        try
                        {
                            id = Convert.ToInt64(conditie);
                            isLong = true;
                        }
                        catch
                        {
                            isLong = false;
                        }
                        if (isLong)
                        {
                            WGAccount account = new WGAccount(Bot.WarGamingAppId, id, false, true, true);
                            if (account != null)
                            {
                                await Bot.ShowMemberInfo(ctx.Channel, account);
                            }
                            else
                            {
                                await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**GebruikersID (`" + id + "`) kon niet gevonden worden!**");
                            }
                        }
                        else
                        {
                            await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Geef een ID!**");
                        }
                    }
                    else
                    {
                        await Bot.SearchPlayer(ctx.Channel, ctx.Member, ctx.User, ctx.Guild.Name, conditie);
                    }
                    await Bot.ConfirmCommandExecuted(ctx.Message);
                }
                else
                {
                    await Bot.SayTheUserIsNotAllowed(ctx.Channel);
                }
            }
        }
        //[Command("hof")]
        //[Aliases("ho", "ha", "hal", "hall", "hallo", "hallof", "halloff", "halloffa", "halloffam", "halloffame")]
        //[Description("Geeft de gebruiker de mogelijkheid om, met een replay door te sturen, een plekje in de Clan Hall Of Fame te krijgen.\nDe replay kan zowel een .wotbreplay bestand zijn als een link van [wotinspector](https://replays.wotinspector.com/en/)")]
        //public async Task hof(CommandContext ctx, params string[] optioneel_titel)
        //{
        //    if (!Bot.ignoreCommands)
        //    {
        //        if (Bot.hasRight(ctx.Member, ctx.Command))
        //        {
        //            DiscordChannel masteryChannel = null;
        //            bool allowIt = false;
        //            if (ctx.Guild.Id.Equals(Bot.DA_BOIS_ID))
        //            {
        //                allowIt = true;
        //            }
        //            else
        //            {
        //                masteryChannel = await Bot.GetMasteryReplaysChannel(ctx.Guild.Id);
        //                if (masteryChannel == null)
        //                {
        //                    await Bot.SaySomethingWentWrong(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Het mastery-replays kanaal kon niet gevonden worden!**");
        //                    allowIt = true;
        //                }
        //                else if (ctx.Channel.Id.Equals(masteryChannel.Id))
        //                {
        //                    allowIt = true;
        //                }
        //                else
        //                {
        //                    DiscordChannel bottestChannel = await Bot.GetBottestChannel();
        //                    if (bottestChannel != null)
        //                    {
        //                        if (ctx.Channel.Id.Equals(bottestChannel.Id))
        //                        {
        //                            allowIt = true;
        //                        }
        //                    }
        //                }
        //            }
        //            if (allowIt)
        //            {
        //                DiscordMessage post = await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Je krijgt 30 seconden om je replay/url in dit channel up te loaden.**");
        //                var interactivity = ctx.Client.GetInteractivity();
        //                var message = await interactivity.WaitForMessageAsync(x => x.Channel == ctx.Channel && x.Author == ctx.User);
        //                if (!message.TimedOut)
        //                {
        //                    await post.DeleteAsync();
        //                    await ctx.Message.DeleteAsync();
        //                    if (message.Result.Attachments.Count > 0)
        //                    {
        //                        bool replayFound = false;
        //                        foreach (DiscordAttachment attachment in message.Result.Attachments)
        //                        {
        //                            if (attachment.FileName.EndsWith(".wotbreplay"))
        //                            {
        //                                replayFound = true;
        //                                StringBuilder sbTitel = new StringBuilder();
        //                                for (int i = 0; i < optioneel_titel.Length; i++)
        //                                {
        //                                    if (i > 0)
        //                                    {
        //                                        sbTitel.Append(' ');
        //                                    }
        //                                    sbTitel.Append(optioneel_titel[i]);
        //                                }
        //                                Tuple<string, DiscordMessage> returnedTuple = await Bot.handle(sbTitel.ToString(), ctx.Channel, ctx.Member, ctx.Guild.Name, ctx.Guild.Id, attachment);
        //                                await Bot.hofAfterUpload(returnedTuple, message.Result);
        //                            break;
        //                            }
        //                        }
        //                        if (!replayFound)
        //                        {
        //                            await Bot.SayWrongAttachments(ctx.Channel, ctx.Member, ctx.Guild.Name);
        //                        }
        //                    }
        //                    else
        //                    {
        //                        //await Bot.SayNoAttachments(ctx);
        //                        bool urlUsed = false;
        //                        if (message.Result != null)
        //                        {
        //                            if (message.Result.Content.StartsWith("http"))
        //                            {
        //                                urlUsed = true;
        //                                string[] splitted = message.Result.Content.Split(' ');
        //                                StringBuilder sbTitel = new StringBuilder();
        //                                if (splitted.Length > 1)
        //                                {
        //                                    for (int i = 1; i < splitted.Length; i++)
        //                                    {
        //                                        if (i > 1)
        //                                        {
        //                                            sbTitel.Append(' ');
        //                                        }
        //                                        sbTitel.Append(splitted[i]);
        //                                    }
        //                                }
        //                                string url = splitted[0];
        //                                Tuple<string, DiscordMessage> returnedTuple = await Bot.handle(sbTitel.ToString(), ctx.Channel, ctx.Member, ctx.Guild.Name, ctx.Guild.Id, url);
        //                                await Bot.hofAfterUpload(returnedTuple, message.Result);
        //                            }
        //                        }
        //                        if (!urlUsed)
        //                        {
        //                            await Bot.SayNoAttachments(ctx.Channel, ctx.Member, ctx.Guild.Name);
        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    await Bot.SayNoResponse(ctx.Channel);
        //                }
        //            }
        //            else
        //            {
        //                string mention = "#masterychannel";
        //                if (masteryChannel != null)
        //                {
        //                    if (masteryChannel.Mention != null)
        //                    {
        //                        mention = masteryChannel.Mention;
        //                    }
        //                    else
        //                    {
        //                        mention = "#" + masteryChannel.Name;
        //                    }
        //                }
        //                await Bot.SaySomethingWentWrong(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Je mag deze commando enkel gebruiken in " + mention + "!**");
        //            }
        //        }
        //        else
        //        {
        //            await Bot.SayTheUserIsNotAllowed(ctx.Channel);
        //        }
        //    }
        //}
        [Command("ResetHOF")]
        [Aliases("res", "rese", "rest", "rst", "rset", "reset")]
        [Description("Verwijdert alle opgeslagen replays in de Hall Of Fame.")]
        public async Task refresh(CommandContext ctx)
        {
            if (!Bot.ignoreCommands)
            {
                if (Bot.HasRight(ctx.Member, ctx.Command))
                {
                    await Bot.ConfirmCommandExecuting(ctx.Message);
                    DiscordChannel channel = await Bot.GetHallOfFameChannel(ctx.Guild.Id);
                    if (channel != null)
                    {
                        bool noErrors = true;
                        List<Tuple<int, DiscordMessage>> tiersFound = new List<Tuple<int, DiscordMessage>>();
                        try
                        {
                            IReadOnlyList<DiscordMessage> messages = await channel.GetMessagesAsync(100);
                            messages.Reverse();
                            foreach (DiscordMessage message in messages)
                            {
                                if (!message.Pinned)
                                {
                                    if (message.Embeds != null)
                                    {
                                        if (message.Embeds.Count > 0)
                                        {
                                            for (int i = 1; i <= 10; i++)
                                            {
                                                bool containsItem = false;
                                                foreach (DiscordEmbed embed in message.Embeds)
                                                {
                                                    if (embed.Title != null)
                                                    {
                                                        if (embed.Title.Contains(Bot.GetDiscordEmoji(Emoj.getName(i))))
                                                        {
                                                            tiersFound.Add(new Tuple<int, DiscordMessage>(i, message));
                                                            containsItem = true;
                                                            break;
                                                        }
                                                    }
                                                }
                                                if (containsItem)
                                                {
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            await Bot.HandleError("While getting the HOF messages (" + ctx.Command.Name + "): ", e.Message, e.StackTrace);
                            noErrors = false;
                        }
                        if (noErrors)
                        {
                            if (await Bot.CreateOrCleanHOFMessages(channel, tiersFound))
                            {
                                await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**De Hall Of Fame is gereset!**");
                            }
                            else
                            {
                                await Bot.SaySomethingWentWrong(ctx.Channel, ctx.Member, ctx.Guild.Name, "**De Hall Of Fame kon de berichten niet resetten!**");
                            }
                        }
                        else
                        {
                            await Bot.SaySomethingWentWrong(ctx.Channel, ctx.Member, ctx.Guild.Name, "**De Hall Of Fame kon niet gereset worden door een interne reden!**");
                        }
                    }
                    else
                    {
                        await Bot.SaySomethingWentWrong(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Hall Of Fame kanaal kon niet gereset worden!**");
                    }
                    await Bot.ConfirmCommandExecuted(ctx.Message);
                }
                else
                {
                    await Bot.SayTheUserIsNotAllowed(ctx.Channel);
                }
            }
        }
        [Command("VerwijderSpelerHOF")]
        [Description("Verwijdert een bepaalde persoon van de HOF. (Hoofdlettergevoelig)")]
        public async Task removePlayerFromHOF(CommandContext ctx, string naam)
        {
            if (!Bot.ignoreCommands)
            {
                if (Bot.HasRight(ctx.Member, ctx.Command))
                {
                    await Bot.ConfirmCommandExecuting(ctx.Message);
                    bool foundAtLeastOnce = false;
                    naam = naam.Replace(Bot.UNDERSCORE_REPLACEMENT_CHAR, '_');
                    naam = naam.Replace('_', Bot.UNDERSCORE_REPLACEMENT_CHAR);
                    DiscordChannel channel = await Bot.GetHallOfFameChannel(ctx.Guild.Id);
                    if (channel != null)
                    {
                        IReadOnlyList<DiscordMessage> messages = await channel.GetMessagesAsync(100);
                        for (int i = 1; i <= 10; i++)
                        {
                            List<DiscordMessage> tempTierMessages = Bot.GetTierMessages(i, messages);
                            foreach (DiscordMessage message in tempTierMessages)
                            {
                                bool playerRemoved = false;
                                List<Tuple<string, List<TankHof>>> tupleList = Bot.ConvertHOFMessageToTupleListAsync(message, i);
                                if (tupleList != null)
                                {
                                    List<Tuple<string, List<TankHof>>> tempTupleList = new List<Tuple<string, List<TankHof>>>();
                                    for (int j = 0; j < tupleList.Count; j++)
                                    {
                                        if (tupleList[j].Item2 != null)
                                        {
                                            List<TankHof> tempTupleListItem2 = new List<TankHof>();
                                            for (int k = 0; k < tupleList[j].Item2.Count; k++)
                                            {
                                                if (!tupleList[j].Item2[k].speler.Equals(naam))
                                                {
                                                    tempTupleListItem2.Add(tupleList[j].Item2[k]);
                                                }
                                                else
                                                {
                                                    playerRemoved = true;
                                                }
                                            }
                                            if (tempTupleListItem2.Count > 0)
                                            {
                                                tempTupleList.Add(new Tuple<string, List<TankHof>>(tempTupleListItem2[0].tank, tempTupleListItem2));
                                            }
                                        }
                                    }
                                    tupleList = tempTupleList;
                                }
                                if (playerRemoved)
                                {
                                    if (!foundAtLeastOnce)
                                    {
                                        foundAtLeastOnce = true;
                                    }
                                    await Bot.EditHOFMessage(message, tupleList);
                                    await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**" + naam + " werd verwijdert uit tier " + Bot.GetDiscordEmoji(Emoj.getName(i)) + "**");
                                }
                            }
                        }
                    }
                    if (!foundAtLeastOnce)
                    {
                        await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Persoon met `" + naam + "` als naam komt niet voor in de HOF.**");
                    }
                    await Bot.ConfirmCommandExecuted(ctx.Message);
                }
                else
                {
                    await Bot.SayTheUserIsNotAllowed(ctx.Channel);
                }
            }
        }
        [Command("HernoemSpelerHOF")]
        [Description("Verandert de naam in de HOF naar een andere naam. (Hoofdlettergevoelig)")]
        public async Task changeHOFName(CommandContext ctx, string oldName, string niewe_naam)
        {
            if (!Bot.ignoreCommands)
            {
                if (Bot.HasRight(ctx.Member, ctx.Command))
                {
                    await Bot.ConfirmCommandExecuting(ctx.Message);
                    bool foundAtLeastOnce = false;
                    oldName = oldName.Replace(Bot.UNDERSCORE_REPLACEMENT_CHAR, '_');
                    oldName = oldName.Replace('_', Bot.UNDERSCORE_REPLACEMENT_CHAR);
                    niewe_naam = niewe_naam.Replace('_', Bot.UNDERSCORE_REPLACEMENT_CHAR);
                    DiscordChannel channel = await Bot.GetHallOfFameChannel(ctx.Guild.Id);
                    if (channel != null)
                    {
                        IReadOnlyList<DiscordMessage> messages = await channel.GetMessagesAsync(100);
                        for (int i = 1; i <= 10; i++)
                        {
                            List<DiscordMessage> tempTierMessages = Bot.GetTierMessages(i, messages);
                            foreach (DiscordMessage message in tempTierMessages)
                            {
                                bool nameChanged = false;
                                List<Tuple<string, List<TankHof>>> tupleList = Bot.ConvertHOFMessageToTupleListAsync(message, i);
                                if (tupleList != null)
                                {
                                    foreach (Tuple<string, List<TankHof>> tupleItem in tupleList)
                                    {
                                        if (tupleItem.Item2 != null)
                                        {
                                            foreach (TankHof tankHofItem in tupleItem.Item2)
                                            {
                                                if (tankHofItem.speler.Equals(oldName))
                                                {
                                                    nameChanged = true;
                                                    tankHofItem.speler = niewe_naam;
                                                }
                                            }
                                        }
                                    }
                                }
                                if (nameChanged)
                                {
                                    if (!foundAtLeastOnce)
                                    {
                                        foundAtLeastOnce = true;
                                    }
                                    await Bot.EditHOFMessage(message, tupleList);
                                    await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**" + oldName + " werd verandert naar " + niewe_naam + " in tier " + Bot.GetDiscordEmoji(Emoj.getName(i)) + "**");
                                }
                            }
                        }
                    }
                    if (!foundAtLeastOnce)
                    {
                        await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Persoon met `" + oldName + "` als naam komt niet voor in de HOF.**");
                    }
                    await Bot.ConfirmCommandExecuted(ctx.Message);
                }
                else
                {
                    await Bot.SayTheUserIsNotAllowed(ctx.Channel);
                }
            }
        }
        [Command("hof")]
        [Aliases("hf")]
        [Description("Geeft een lijst van de spelers die in de Hall of Fame voorkomen.")]
        public async Task hof(CommandContext ctx)
        {
            if (!Bot.ignoreCommands)
            {
                if (Bot.HasRight(ctx.Member, ctx.Command))
                {
                    await Bot.ConfirmCommandExecuting(ctx.Message);
                    List<Tuple<string, List<TankHof>>> playerList = await Bot.GetTankHofsPerPlayer(ctx.Guild.Id);
                    playerList = playerList.OrderBy(x => x.Item2.Count).ToList();
                    playerList.Reverse();
                    StringBuilder sb = new StringBuilder("```");
                    bool firstTime = true;
                    foreach (Tuple<string, List<TankHof>> player in playerList)
                    {
                        if (firstTime)
                        {
                            firstTime = false;
                        }
                        else
                        {
                            sb.Append("\n");
                        }
                        sb.Append(player.Item1.Replace(Bot.UNDERSCORE_REPLACEMENT_CHAR, '_'));
                        for (int i = player.Item1.Length; i < MAX_NAME_LENGTH_IN_WOTB + 7; i++) //25 = max name length in wotb (minimum 2)
                        {
                            sb.Append(" ");
                        }
                        sb.Append(player.Item2.Count.ToString());
                    }
                    sb.Append("```");
                    await Bot.CreateEmbed(ctx.Channel, string.Empty, string.Empty, "Hall Of Fame plekken per speler", sb.ToString(), null, null, string.Empty, null);
                    await Bot.ConfirmCommandExecuted(ctx.Message);
                }
            }
        }
        [Command("hofplayer")]
        [Aliases("hofp", "hp", "hofplaye", "hofplay", "hofpla", "hofpl", "hfplayer")]
        [Description("Geeft een lijst van plekken dat de speler in de Hall Of Fame gehaald heeft.")]
        public async Task hofplayer(CommandContext ctx, string name)
        {
            if (!Bot.ignoreCommands)
            {
                if (Bot.HasRight(ctx.Member, ctx.Command))
                {
                    name = name.Replace('_', Bot.UNDERSCORE_REPLACEMENT_CHAR);
                    await Bot.ConfirmCommandExecuting(ctx.Message);
                    List<Tuple<string, List<TankHof>>> playerList = await Bot.GetTankHofsPerPlayer(ctx.Guild.Id);
                    List<DEF> defList = new List<DEF>();
                    playerList.Reverse();
                    bool found = false;
                    StringBuilder sb = new StringBuilder();
                    foreach (Tuple<string, List<TankHof>> player in playerList)
                    {
                        if (name.ToLower().Equals(player.Item1.ToLower()))
                        {
                            List<int> alreadyUsedTiers = new List<int>();
                            List<string> alreadyUsedTanks = new List<string>();
                            bool firstTime = true;
                            int lastTier = 0;
                            foreach (TankHof tank in player.Item2)
                            {
                                if (!alreadyUsedTiers.Contains(tank.tier))
                                {
                                    alreadyUsedTiers.Add(tank.tier);
                                    alreadyUsedTanks = new List<string>();
                                    if (lastTier > 0)
                                    {
                                        sb.Append("```");
                                        DEF tempDef = new DEF();
                                        tempDef.Name = "Tier " + lastTier;
                                        tempDef.Inline = false;
                                        tempDef.Value = sb.ToString();
                                        defList.Add(tempDef);
                                        sb = new StringBuilder();
                                    }
                                    sb.Append("```");
                                }
                                if (firstTime)
                                {
                                    firstTime = false;
                                }
                                else
                                {
                                    sb.Append("\n");
                                }
                                if (!alreadyUsedTanks.Contains(tank.tank))
                                {
                                    alreadyUsedTanks.Add(tank.tank);
                                    if (alreadyUsedTanks.Count > 1)
                                    {
                                        sb.Append("\n");
                                    }
                                }
                                if (tank.tank.Length > MAX_TANK_NAME_LENGTH_IN_WOTB)
                                {
                                    sb.Append(tank.tank.Substring(0, MAX_TANK_NAME_LENGTH_IN_WOTB));
                                }
                                else
                                {
                                    sb.Append(tank.tank);
                                }
                                for (int i = (tank.tank.Length < MAX_TANK_NAME_LENGTH_IN_WOTB ? tank.tank.Length : MAX_TANK_NAME_LENGTH_IN_WOTB); i < MAX_TANK_NAME_LENGTH_IN_WOTB + 2; i++)
                                {
                                    sb.Append(" ");
                                }
                                sb.Append("nr." + tank.place.ToString());
                                for (int i = 0; i < 7; i++)
                                {
                                    sb.Append(" ");
                                }
                                sb.Append(tank.damage + " dmg");
                                lastTier = tank.tier;
                            }
                            sb.Append("```");
                            DEF tempDef2 = new DEF();
                            tempDef2.Name = "Tier " + lastTier;
                            tempDef2.Inline = false;
                            tempDef2.Value = sb.ToString();
                            defList.Add(tempDef2);
                            found = true;
                            break;
                        }
                    }
                    await Bot.CreateEmbed(ctx.Channel, string.Empty, string.Empty, "Hall Of Fame plekken van " + name.Replace(Bot.UNDERSCORE_REPLACEMENT_CHAR, '_'), (found ? string.Empty : "Deze speler heeft nog geen plekken in de Hall Of Fame gehaald."), defList, null, string.Empty, null);
                    await Bot.ConfirmCommandExecuted(ctx.Message);
                }
            }
        }
        #region UpdateGebruikers
        //[Command("UpdateGebruikers")]
        //[Aliases("ug")]
        //[Description("Updatet de bijnamen van alle gebruikers naargelang hun WoTB gebruikersnaam en clan.")]
        //public async Task updateUsers(CommandContext ctx)
        //{
        //    if (!Bot.ignoreCommands)
        //    {
        //        if (Bot.hasRight(ctx.Member, ctx.Command))
        //        {
        //            await Bot.confirmCommandExecuting(ctx.Message);
        //            await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Ik ga de bijnamen controleren... :hourglass: **");
        //            Stopwatch timer = new Stopwatch();

        //            timer.Start();
        //            //HIER GEKNIPT

        //            /*
        //            IReadOnlyCollection<DiscordMember> members = await ctx.Guild.GetAllMembersAsync();
        //            foreach (DiscordMember member in members)
        //            {
        //                if (!member.IsBot)
        //                {
        //                    if (member.Roles != null)
        //                    {
        //                        if (member.Roles.Contains(ctx.Guild.GetRole(Bot.LEDEN_ROLE)))
        //                        {
        //                            bool accountFound = false;
        //                            Tuple<string, string> gebruiker = Bot.getIGNFromMember(member.DisplayName);
        //                            IReadOnlyList<WGAccount> wgAccounts = await WGAccount.searchByName(SearchAccuracy.EXACT, gebruiker.Item2, Bot_warGamingAppId, false, true, false);
        //                            if (wgAccounts != null)
        //                            {
        //                                if (wgAccounts.Count > 0)
        //                                {
        //                                    //Account met exact deze gebruikersnaam gevonden
        //                                    accountFound = true;
        //                                    bool goodClanTag = false;
        //                                    string clanTag = string.Empty;
        //                                    if (gebruiker.Item1.Length > 1)
        //                                    {
        //                                        if (gebruiker.Item1.StartsWith('[') && gebruiker.Item1.EndsWith(']'))
        //                                        {
        //                                            goodClanTag = true;
        //                                            string currentClanTag = string.Empty;
        //                                            if (wgAccounts[0].clan != null)
        //                                            {
        //                                                if (wgAccounts[0].clan.tag != null)
        //                                                {
        //                                                    currentClanTag = wgAccounts[0].clan.tag;
        //                                                }
        //                                            }
        //                                            string goodDisplayName = '[' + currentClanTag + "] " + wgAccounts[0].nickname;
        //                                            if (!member.DisplayName.Equals(goodDisplayName))
        //                                            {
        //                                                await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Gaat bijnaam van **`" + member.DisplayName + "`** aanpassen naar **`" + goodDisplayName + "`");
        //                                                await Bot.changeMemberNickname(member, goodDisplayName);
        //                                            }
        //                                        }
        //                                    }
        //                                    if (!goodClanTag)
        //                                    {
        //                                        if (wgAccounts[0].clan != null)
        //                                        {
        //                                            if (wgAccounts[0].clan.tag != null)
        //                                            {
        //                                                clanTag = wgAccounts[0].clan.tag;
        //                                            }
        //                                        }
        //                                        string goodDisplayName = '[' + clanTag + "] " + wgAccounts[0].nickname;
        //                                        await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Gaat bijnaam van **`" + member.DisplayName + "`** aanpassen naar **`" + goodDisplayName + "`");
        //                                        await Bot.changeMemberNickname(member, goodDisplayName);
        //                                    }
        //                                }
        //                            }
        //                            if (!accountFound)
        //                            {
        //                                await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Bijnaam van **`" + member.DisplayName + "`** komt niet overeen met WoTB account.**");
        //                                await Bot.SendPrivateMessage(member, ctx.Guild.Name, "Hallo,\n\nEr werd voor iedere gebruiker in de NLBE discord server gecontroleerd of je bijnaam overeenkomt met je wargaming account.\nHelaas is dit voor jou niet het geval.\nZou je dit zelf even willen aanpassen aub?\nPas je bijnaam aan naargelang de vereisten het #regels kanaal.\n\nAlvast bedankt!\n- [NLBE] sjtubbers#4241");
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //            */

        //            timer.Stop();
        //            await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Ik ben, na `" + (timer.ElapsedMilliseconds / 1000) + "`s, klaar met het controleren van de bijnamen! :white_check_mark:**");
        //            await Bot.confirmCommandExecuted(ctx.Message);
        //        }
        //        else
        //        {
        //            await Bot.SayTheUserIsNotAllowed(ctx.Channel);
        //        }
        //    }
        //}
        #endregion
        #region ListGebruikers
        // [Command("ListGebruikers")]
        // [Aliases("lg")]
        // [Description("Lijst de bijnamen van alle gebruikers op naargelang hun WoTB gebruikersnaam en clan.")]
        // public async Task updateUsers(CommandContext ctx)
        // {
        //     if (!Bot.ignoreCommands)
        //     {
        //         if (Bot.hasRight(ctx.Member, ctx.Command))
        //         {
        //             await Bot.confirmCommandExecuting(ctx.Message);
        //             
        //             IReadOnlyCollection<DiscordMember> members = await ctx.Guild.GetAllMembersAsync();
        //             StringBuilder sb = new StringBuilder();
        //             foreach (DiscordMember member in members)
        //             {
        //                 if (!member.IsBot)
        //                 {
        //                     if (member.Roles != null)
        //                     {
        //                         if (member.Roles.Contains(ctx.Guild.GetRole(Bot.LEDEN_ROLE)))
        //                         {
        //                             IReadOnlyList<WGAccount> wgAccounts = await WGAccount.searchByName(SearchAccuracy.EXACT, member.DisplayName.Split("] ")[1], Bot_warGamingAppId, false, true, false);
        //                             if (wgAccounts != null)
        //                             {
        //                                 if (wgAccounts.Count > 0)
        //                                 {
        //                                     string currentClanTag = string.Empty;
        //                                     if (wgAccounts[0].clan != null)
        //                                     {
        //                                         if (wgAccounts[0].clan.tag != null)
        //                                         {
        //                                             currentClanTag = wgAccounts[0].clan.tag;
        //                                         }
        //                                     }
        //                                     string goodDisplayName = '[' + currentClanTag + "] " + wgAccounts[0].nickname;
        //                                     sb.AppendLine(goodDisplayName);
        //                                 }
        //                             }
        //                         }
        //                     }
        //                 }
        //             }
        //             
        //             //TODO: actually send the message with sb as content
        //
        //             await Bot.confirmCommandExecuted(ctx.Message);
        //         }
        //         else
        //         {
        //             await Bot.SayTheUserIsNotAllowed(ctx.Channel);
        //         }
        //     }
        // }
        #endregion
        #region genereerlog
        //[Command("GenLog")]
        //[Description("Genereert log obv votes.")]
        //public async Task generateLog(CommandContext ctx, params string[] optioneel_hoeveelste_bericht)
        //{
        //    if (!Bot.ignoreCommands)
        //    {
        //        if (Bot.hasRight(ctx.Member, ctx.Command))
        //        {
        //            await Bot.confirmCommandExecuting(ctx.Message);
        //            int hoeveelsteBericht = 1;
        //            bool isInt = true;
        //            if (optioneel_hoeveelste_bericht.Length > 0)
        //            {
        //                try
        //                {
        //                    hoeveelsteBericht = Convert.ToInt32(optioneel_hoeveelste_bericht[0]);
        //                }
        //                catch
        //                {
        //                    isInt = false;
        //                }
        //            }
        //            if (isInt)
        //            {
        //                if (hoeveelsteBericht == 0)
        //                {
        //                    hoeveelsteBericht = 1;
        //                }
        //                DiscordChannel theChannel = await Bot.GetToernooiAanmeldenChannel(ctx.Guild.Id);
        //                if (theChannel != null)
        //                {
        //                    hoeveelsteBericht--;
        //                    IReadOnlyList<DiscordMessage> messages = await theChannel.GetMessagesAsync(20);
        //                    for (int i = 0; i < messages.Count; i++)
        //                    {
        //                        if (i == hoeveelsteBericht)
        //                        {
        //                            for (int tier = 1; tier <= 10; tier++)
        //                            {
        //                                IReadOnlyList<DiscordUser> reactions = await messages[i].GetReactionsAsync(Bot.getDiscordEmoji(Emoj.getName(tier)));
        //                                foreach (DiscordUser user in reactions)
        //                                {
        //                                    if (!user.IsBot)
        //                                    {
        //                                        await Bot.generateLogMessage(messages[i], theChannel, user.Id, Bot.getDiscordEmoji(Emoj.getName(tier)));
        //                                    }
        //                                }
        //                            }
        //                            break;
        //                        }
        //                    }
        //                }
        //            }
        //            await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Log gegenereerd!**");
        //            await Bot.confirmCommandExecuted(ctx.Message);
        //        }
        //    }
        //}
        #endregion
        #region NextTournament
        //[Command("NextTournament")]
        //[Aliases("nt")]
        //[Description("Geeft de volgende toernooi.")]
        //public async Task nextTournament(CommandContext ctx, params string[] optioneel_nummer)
        //{
        //    await Bot.confirmCommandExecuting(ctx.Message);
        //    if (optioneel_nummer.Length <= 1)
        //    {
        //        bool isInt = true;
        //        int theNumber = 1;
        //        if (optioneel_nummer.Length > 0)
        //        {
        //            try
        //            {
        //                theNumber = Convert.ToInt32(optioneel_nummer[0]);
        //                if (theNumber <= 0)
        //                {
        //                    isInt = false;
        //                }
        //            }
        //            catch
        //            {
        //                isInt = false;
        //            }
        //        }
        //        if (isInt)
        //        {
        //            theNumber--;
        //            List<WGTournament> tournamentsList = await Bot.initialiseTournaments(false);
        //            if (tournamentsList.Count > 0)
        //            {
        //                if (tournamentsList.Count > theNumber)
        //                {
        //                    if (theNumber >= 0)
        //                    {
        //                        string titel = string.Empty;
        //                        if (theNumber == 0)
        //                        {
        //                            titel = "Eerstvolgende toernooi";
        //                        }
        //                        else
        //                        {
        //                            titel = (theNumber + 1) + "de komende toernooi";
        //                        }
        //                        await Bot.showTournamentInfo(ctx.Channel, tournamentsList[theNumber], titel);
        //                    }
        //                    else
        //                    {
        //                        await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Kies een groter getal dan 0.**");
        //                    }
        //                }
        //                else
        //                {
        //                    await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Zoveel toernooien zijn er nog niet gepland in de toekomst.**");
        //                }
        //            }
        //            else
        //            {
        //                await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**De komende toernooien konden niet opgelijst worden.**");
        //            }
        //        }
        //    }
        //    else
        //    {
        //        await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Je mag maar 1 extra waarde meegeven.**");
        //    }
        //    await Bot.confirmCommandExecuted(ctx.Message);
        //}
        #endregion
        #region Toernooien
        [Command("Toernooien")]
        [Aliases("trn")]
        [Description("Geeft zowel de recente toernooien als de komende toernooien.")]
        public async Task toernooien(CommandContext ctx, params string[] optioneel_nummer)
        {
            await Bot.ConfirmCommandExecuting(ctx.Message);
            if (optioneel_nummer.Length <= 1)
            {
                bool isInt = true;
                int theNumber = 1;
                if (optioneel_nummer.Length > 0)
                {
                    try
                    {
                        theNumber = Convert.ToInt32(optioneel_nummer[0]);
                        if (theNumber <= 0)
                        {
                            isInt = false;
                        }
                    }
                    catch
                    {
                        isInt = false;
                    }
                }
                else
                {
                    isInt = false;
                }
                theNumber--;
                List<WGTournament> tournamentsList = await Bot.InitialiseTournaments(true);
                if (isInt)
                {
                    await Bot.ShowTournamentInfo(ctx.Channel, tournamentsList[theNumber], (theNumber + 1) + (theNumber == 0 ? "ste" : "de") + " toernooi");
                }
                else
                {
                    if (tournamentsList.Count > 0)
                    {
                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < tournamentsList.Count; i++)
                        {
                            int laagste = -1;
                            int hoogste = -1;
                            if (tournamentsList[i].stages != null)
                            {
                                foreach (Stage stage in tournamentsList[i].stages)
                                {
                                    if (laagste > stage.min_tier)
                                    {
                                        laagste = stage.min_tier;
                                    }
                                    if (hoogste < stage.max_tier)
                                    {
                                        hoogste = stage.max_tier;
                                        if (laagste == -1)
                                        {
                                            laagste = hoogste;
                                        }
                                    }
                                }
                            }
                            sb.AppendLine((i + 1) + ": " + tournamentsList[i].title + (tournamentsList[i].stages != null ? " -> Tier" + (hoogste != laagste ? "s" : "") + ": " + laagste + (laagste != hoogste ? " - " + hoogste : "") : "") + " -> Registraties: " + tournamentsList[i].registration_start_at + " - " + tournamentsList[i].registration_end_at + "\n");
                        }
                        await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, sb.ToString());
                    }
                    else
                    {
                        await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Geen enkel toernooi kon ingeladen worden.**");
                    }
                }
            }
            else
            {
                await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Je mag maar 1 extra waarde meegeven.**");
            }
            await Bot.ConfirmCommandExecuted(ctx.Message);
        }
        #endregion
        #region Bonus Code
        [Command("Bonuscode")]
        [Aliases("bc", "boncode", "bonscode", "bonc", "bonuscod", "bonusco", "bonusc", "bonus", "bonu", "bon", "bo", "b")]
        [Description("Geeft de link om een bonuscode in te vullen (enkel nodig voor pc spelers, anderen kunnen dit via het spel doen).")]
        public async Task Bonuscode(CommandContext ctx)
        {
            if (!Bot.ignoreCommands)
            {
                if (Bot.HasRight(ctx.Member, ctx.Command))
                {
                    await Bot.ConfirmCommandExecuting(ctx.Message);
                    await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name,
                        "**Redeem code:**\nhttps://eu.wargaming.net/shop/redeem/?utm_content=bonus-code&utm_source=global-nav&utm_medium=link&utm_campaign=wotb-portal");
                    await Bot.ConfirmCommandExecuted(ctx.Message);
                }
                else
                {
                    await Bot.SayTheUserIsNotAllowed(ctx.Channel);
                }
            }
        }
        #endregion
        #region Weekly Event
        [Command("Weekly")]
        [Description("Start het proces van het instellen van de tank voor het wekelijkse event.")]
        public async Task Weekly(CommandContext ctx, params string[] optioneel_tank_naam)
        {
            if (!Bot.ignoreCommands)
            {
                if (Bot.HasRight(ctx.Member, ctx.Command))
                {
                    await Bot.ConfirmCommandExecuting(ctx.Message);
                    if (optioneel_tank_naam.Length > 0)
                    {
                        StringBuilder sb = new StringBuilder();
                        for (short i = 0; i < optioneel_tank_naam.Length; i++)
                        {
                            if (i > 0)
                            {
                                sb.Append(' ');
                            }
                            sb.Append(optioneel_tank_naam[i]);
                        }
                        WeeklyEventHandler weeklyEventHandler = new WeeklyEventHandler();
                        await weeklyEventHandler.CreateNewWeeklyEvent(sb.ToString(), await Bot.GetWeeklyEventChannel());
                    }
                    else
                    {
                        await ctx.Member.SendMessageAsync("Hallo\nWelke tank wil je bij het volgende wekelijkse event instellen?"); //deze triggert OOK het dmchannelcreated event
                        Bot.weeklyEventWinner = new Tuple<ulong, DateTime>(ctx.Member.Id, DateTime.Now);
                    }
                    await Bot.ConfirmCommandExecuted(ctx.Message);
                }
                else
                {
                    await Bot.SayTheUserIsNotAllowed(ctx.Channel);
                }
            }
        }
        #endregion
        #region Weekly Event demo
        //[Command("Weeklydemo")]
        //[Aliases("wd")]
        //[Description("Stelt een demo in voor het wekelijkse event. Puur om het mezel makkeljk te maken")]
        //public async Task Weeklydemo(CommandContext ctx)
        //{
        //    if (!Bot.ignoreCommands)
        //    {
        //        if (Bot.hasRight(ctx.Member, ctx.Command))
        //        {
        //            await Bot.confirmCommandExecuting(ctx.Message);
        //            //verwijder alle berichten in weeklyeventchannel
        //            DiscordChannel weeklyEventChannel = await Bot.GetWeeklyEventChannel();
        //            IReadOnlyList<DiscordMessage> messages = await weeklyEventChannel.GetMessagesAsync();
        //            if (messages.Count > 0)
        //            {
        //                await weeklyEventChannel.DeleteMessagesAsync(messages);
        //            }
        //            WeeklyEventHandler weeklyEventHandler = new WeeklyEventHandler();
        //            string replayString = await Bot.replayToString("https://replays.wotinspector.com/en/view/15cbffa9caf76c3e944af9182bd04020", string.Empty, null);
        //            WGBattle battle = new WGBattle(replayString);
        //            await weeklyEventHandler.CreateNewWeeklyEvent(battle.vehicle, await Bot.GetWeeklyEventChannel()); //initialiseer een nieuwe tank voor het wekelijkse event (Grille 15 in dit geval)
        //            await weeklyEventHandler.GetStringForWeeklyEvent(battle); //voeg een battle toe aan het weklijkse event

        //            //selecteer de winner
        //            await Bot.WeHaveAWinner(ctx.Guild, weeklyEventHandler.WeeklyEvent.WeeklyEventItems[0], battle.vehicle);
        //            await Bot.confirmCommandExecuted(ctx.Message);
        //        }
        //        else
        //        {
        //            await Bot.SayTheUserIsNotAllowed(ctx.Channel);
        //        }
        //    }
        //}
        #endregion
        #region gethoflinks
        //[Command("gethoflinks")]
        //public async Task gethoflinks(CommandContext ctx)
        //{
        //    await Bot.confirmCommandExecuting(ctx.Message);
        //    DiscordChannel channel = await Bot.GetHallOfFameChannel(ctx.Guild.Id);
        //    if (channel != null)
        //    {
        //        List<string> urlList = new List<string>();
        //        IReadOnlyList<DiscordMessage> tempMessages = await channel.GetMessagesAsync(100);
        //        for (int i = 1; i <= 10; i++)
        //        {
        //            List<DiscordMessage> messages = Bot.getTierMessages(i, tempMessages);
        //            foreach (DiscordMessage message in messages)
        //            {
        //                List<Tuple<string, List<TankHof>>> iets = Bot.convertHOFMessageToTupleListAsync(message, i);
        //                if (iets != null)
        //                {
        //                    foreach (var item in iets)
        //                    {
        //                        foreach (var x in item.Item2)
        //                        {
        //                            urlList.Add(x.link);
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        if (urlList.Count > 0)
        //        {
        //            StringBuilder sb = new StringBuilder();
        //            foreach (string line in urlList)
        //            {
        //                sb.AppendLine(line);
        //            }
        //            if (!File.Exists(Bot.logInputPath))
        //            {
        //                File.Create(Bot.logInputPath);
        //            }
        //            File.WriteAllText(Bot.logInputPath, sb.ToString());
        //            await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Hof links zijn opgeslagen in apart bestand.**");
        //        }
        //        else
        //        {
        //            await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Er konden geen hof links gevonden worden.**");
        //        }
        //    }
        //    await Bot.confirmCommandExecuted(ctx.Message);
        //}
        #endregion
        #region authenticatie
        //[Command("authenticatie")]
        //public async Task login(CommandContext ctx)
        //{
        //await Bot.confirmCommandExecuting(ctx.Message);
        //    Authentication authentication = new Authentication(Bot.fmwotb);
        //    string description = "Om persoonlijke gegevens te willen gebruiken moet je inloggen met je wargaming account.\n" +
        //        "Enkel dan laat je deze bot toe om deze gegevens te gebruiken.\n\n" +
        //        "**Klik [hier](" + authentication.getLoginURL() + ")** om in te loggen.\n" +
        //        "**Klik [hier](" + authentication.getLogoutURL() + ")** om uit te loggen.\n\n" +
        //        "Indien je een `access token` gekregen hebt, stuur het dan in de chat om de bot er gebruik van te laten maken.";
        //    await Bot.CreateEmbed(ctx.Channel, string.Empty, string.Empty, "Login", description, null, null, string.Empty, null);
        //    Tuple<string, string> iets = await authentication.openIDLogin();
        //       var interactivity = ctx.Client.GetInteractivity();
        //    InteractivityResult<DiscordMessage> message = await interactivity.WaitForMessageAsync(x => x.Channel == ctx.Channel && x.Author == ctx.User);
        //    if (!message.TimedOut)
        //    {
        //        Tuple<string, string> response = await authentication.prolongateAccess(message.Result.Content);
        //        bool ok = true;
        //    }
        //await Bot.confirmCommandExecuted(ctx.Message);
        //}
        #endregion
        #region urep
        //[Command("urep")]
        //public async Task urep(CommandContext ctx, int tier)
        //{
        //    await Bot.confirmCommandExecuting(ctx.Message);
        //    List<string> urlList = new List<string>();
        //    if (tier == 8)
        //    {
        //        #region unieke
        //        urlList.Add("https://replays.wotinspector.com/en/view/d2fa48a43dd6329ff91ceec2c25e8072");
        //        urlList.Add("https://replays.wotinspector.com/en/view/f5304512bf3c0c5ab24be514f7d7844f");
        //        urlList.Add("https://replays.wotinspector.com/en/view/f68aa7679c3843ec273f476f694e158f");
        //        urlList.Add("https://replays.wotinspector.com/en/view/b62472d11e1b67f172ed30289379b739");
        //        urlList.Add("https://replays.wotinspector.com/en/view/69b0487dddce62b29249c0af3988cbaa");
        //        urlList.Add("https://replays.wotinspector.com/en/view/ef9627f76e66918c9bc5a27e95c091fa");
        //        urlList.Add("https://replays.wotinspector.com/en/view/9dcc5ff54c4984526e58b162c9cc9ef5");
        //        urlList.Add("https://replays.wotinspector.com/en/view/26abe67800f47b65b9ffdcf6c1a5a64f");
        //        urlList.Add("https://replays.wotinspector.com/en/view/ea571fe25d334eb89e28115b2e5d5d31");
        //        urlList.Add("https://replays.wotinspector.com/en/view/332a5b36ce340c420f7ae672910a21e8");

        //        urlList.Add("https://replays.wotinspector.com/en/view/e4614acaa062e60fff85d123df4c121b");
        //        urlList.Add("https://replays.wotinspector.com/en/view/6095a2b691358f4c3faa2e2828fefe6e");
        //        urlList.Add("https://replays.wotinspector.com/en/view/d2a590b0ad827ca1e5ee2fcab315a892");
        //        urlList.Add("https://replays.wotinspector.com/en/view/a898549be76f1cc40e7c6602fa079ee6");
        //        urlList.Add("https://replays.wotinspector.com/en/view/d5408503c5ccb080afdb57f6a3a380db");
        //        urlList.Add("https://replays.wotinspector.com/en/view/2aca2441201364b12c857fcb5a7b5be5");
        //        urlList.Add("https://replays.wotinspector.com/en/view/ef62e3e911745fd5079abd6ec51a2694");
        //        urlList.Add("https://replays.wotinspector.com/en/view/1aa7a6a1084cd3ca1045ab671982684e");
        //        urlList.Add("https://replays.wotinspector.com/en/view/19eaa8d6ddb7caa794b271675b8500bf");
        //        urlList.Add("https://replays.wotinspector.com/en/view/f4c283c21a21227457255e03b6348706");

        //        urlList.Add("https://replays.wotinspector.com/en/view/32e5b4deaf6ef21ecb5484f9dbce0987");
        //        urlList.Add("https://replays.wotinspector.com/en/view/465f03e71ceecdcc1ec0b3cdd069a5ca");
        //        urlList.Add("https://replays.wotinspector.com/en/view/d2a590b0ad827ca1e5ee2fcab315a892");
        //        urlList.Add("https://replays.wotinspector.com/en/view/d7db39e5fca9a1ed4274d8065d5be2d8");
        //        urlList.Add("https://replays.wotinspector.com/en/view/f93712e374520c0a8cb0f3b5a665ee79");

        //        urlList.Add("https://replays.wotinspector.com/en/view/a42be76f91b407b2ac8f5ba8c86cdc8c");
        //        urlList.Add("https://replays.wotinspector.com/en/view/c3d7c6a6082582fa64ae97fb99d8ed3a");
        //        urlList.Add("https://replays.wotinspector.com/en/view/d2fa48a43dd6329ff91ceec2c25e8072");
        //        urlList.Add("https://replays.wotinspector.com/en/view/f5304512bf3c0c5ab24be514f7d7844f");
        //        urlList.Add("https://replays.wotinspector.com/en/view/f68aa7679c3843ec273f476f694e158f");

        //        urlList.Add("https://replays.wotinspector.com/en/view/b62472d11e1b67f172ed30289379b739");
        //        urlList.Add("https://replays.wotinspector.com/en/view/a898549be76f1cc40e7c6602fa079ee6");
        //        #endregion
        //    }
        //    else if (tier == 1)
        //    {
        //        #region unieke
        //        urlList.Add("https://replays.wotinspector.com/en/view/c3dc2d2acdceaa013e8210c7a2180251");
        //        urlList.Add("https://replays.wotinspector.com/en/view/074817dcd5ba89704cdd66af11dd6b80");
        //        urlList.Add("https://replays.wotinspector.com/en/view/1aee766e1080893d94a47b79cbd29531");
        //        urlList.Add("https://replays.wotinspector.com/en/view/0187e4156bae1c80f5099043ffd0166b");
        //        urlList.Add("https://replays.wotinspector.com/en/view/e1d04acdb1de194f1562c08e242a6748");
        //        urlList.Add("https://replays.wotinspector.com/en/view/4e7f4a1a6802b2dce0a6a879cdb3312a");
        //        urlList.Add("https://replays.wotinspector.com/en/view/63c1993dc4028052193553d69ecaefa7");
        //        urlList.Add("https://replays.wotinspector.com/en/view/a185886e5d5bd2fc24b2c2884f095aff");
        //        urlList.Add("https://replays.wotinspector.com/en/view/21b5ce084771d79516da5398932eb6f5");
        //        urlList.Add("https://replays.wotinspector.com/en/view/f405e5ef6a41bb972672f8a51b8fbc67");

        //        urlList.Add("https://replays.wotinspector.com/en/view/7eedd61e55f734825f65f06f88754402");
        //        urlList.Add("https://replays.wotinspector.com/en/view/8b1c2c9424b0c35466010595c52fe603");
        //        urlList.Add("https://replays.wotinspector.com/en/view/13d3dd83d6dd6700803bcdd5adc2a5a8");
        //        #endregion
        //    }
        //    else if (tier == 0)
        //    {
        //        urlList.Add("https://replays.wotinspector.com/en/view/8e62f157b16c811bee9ec20eea33f0db");
        //        urlList.Add("https://replays.wotinspector.com/en/view/3fc24f2cf010c9692d028a03b45a5d22");
        //        urlList.Add("https://replays.wotinspector.com/en/view/52e271b50fb2033aa269a39e869037a0");
        //        urlList.Add("https://replays.wotinspector.com/en/view/d5408503c5ccb080afdb57f6a3a380db");
        //        urlList.Add("https://replays.wotinspector.com/en/view/f2a3abe84f2b9e782cbcd05e88af2b3b");
        //        urlList.Add("https://replays.wotinspector.com/en/view/a9b5054beb468709eceea1316d46e9e0");
        //        urlList.Add("https://replays.wotinspector.com/en/view/1a7aac3ce71f6bda1a181c7992473f7f");
        //        urlList.Add("https://replays.wotinspector.com/en/view/91343fe26e8f14674879c979f42c6194");
        //        urlList.Add("https://replays.wotinspector.com/en/view/e2f93bd270a08d3ccb66bc2c17ee2a78");
        //        urlList.Add("https://replays.wotinspector.com/en/view/2b586ed0ccd473fdaf78afa842b8df11");
        //        urlList.Add("https://replays.wotinspector.com/en/view/a42be76f91b407b2ac8f5ba8c86cdc8c");
        //        urlList.Add("https://replays.wotinspector.com/en/view/ba92da89d26da233122a6faf65410100");
        //        urlList.Add("https://replays.wotinspector.com/en/view/4cbbd76d87ada306293bec3459b4ee56");
        //        urlList.Add("https://replays.wotinspector.com/en/view/6671184d4ee70800d41b03acc293a43d");
        //        urlList.Add("https://replays.wotinspector.com/en/view/cba4b6614e6d98a984806dcb7c8d5f45");
        //        urlList.Add("https://replays.wotinspector.com/en/view/9149aecf46514302014e1439196a7f24");
        //        urlList.Add("https://replays.wotinspector.com/en/view/199dae9f00f25541415600d7378d5646");
        //        urlList.Add("https://replays.wotinspector.com/en/view/2aca2441201364b12c857fcb5a7b5be5");
        //        urlList.Add("https://replays.wotinspector.com/en/view/a68d2c19415f887d101299788c1f7483");
        //        urlList.Add("https://replays.wotinspector.com/en/view/5f748d18070e58acb80336ebdf78f6b2");
        //        urlList.Add("https://replays.wotinspector.com/en/view/15d10cef94d23624dc253097dc161c4e");
        //        urlList.Add("https://replays.wotinspector.com/en/view/ef62e3e911745fd5079abd6ec51a2694");
        //        urlList.Add("https://replays.wotinspector.com/en/view/1aa7a6a1084cd3ca1045ab671982684e");
        //        urlList.Add("https://replays.wotinspector.com/en/view/e4bd2369b60f572399872565828f2239");
        //        urlList.Add("https://replays.wotinspector.com/en/view/19eaa8d6ddb7caa794b271675b8500bf");
        //        urlList.Add("https://replays.wotinspector.com/en/view/f4c283c21a21227457255e03b6348706");
        //        urlList.Add("https://replays.wotinspector.com/en/view/32e5b4deaf6ef21ecb5484f9dbce0987");
        //        urlList.Add("https://replays.wotinspector.com/en/view/ef2f2f0d64647699f83ea14a72c72615");
        //        urlList.Add("https://replays.wotinspector.com/en/view/aa9aeed136e57f129a096ccdb4795d57");
        //        urlList.Add("https://replays.wotinspector.com/en/view/bb588938b02f06af4007cd221ee7aa43");
        //        urlList.Add("https://replays.wotinspector.com/en/view/058cf4dfe897606a02112e572faeb1c0");
        //        urlList.Add("https://replays.wotinspector.com/en/view/1b59192cfda68f173d8b603c69cedfa8");
        //        urlList.Add("https://replays.wotinspector.com/en/view/ad499f2e37a96d5a48c41fb9973cb780");
        //        urlList.Add("https://replays.wotinspector.com/en/view/465f03e71ceecdcc1ec0b3cdd069a5ca");
        //        urlList.Add("https://replays.wotinspector.com/en/view/d2a590b0ad827ca1e5ee2fcab315a892");
        //        urlList.Add("https://replays.wotinspector.com/en/view/d7db39e5fca9a1ed4274d8065d5be2d8");
        //        urlList.Add("https://replays.wotinspector.com/en/view/f93712e374520c0a8cb0f3b5a665ee79");
        //        urlList.Add("https://replays.wotinspector.com/en/view/a42be76f91b407b2ac8f5ba8c86cdc8c");
        //        urlList.Add("https://replays.wotinspector.com/en/view/c3d7c6a6082582fa64ae97fb99d8ed3a");
        //        urlList.Add("https://replays.wotinspector.com/en/view/d2fa48a43dd6329ff91ceec2c25e8072");
        //        urlList.Add("https://replays.wotinspector.com/en/view/f79b798f29e8f3b80f0b933ac83c4e2f");
        //        urlList.Add("https://replays.wotinspector.com/en/view/f5304512bf3c0c5ab24be514f7d7844f");
        //        urlList.Add("https://replays.wotinspector.com/en/view/f68aa7679c3843ec273f476f694e158f");
        //        urlList.Add("https://replays.wotinspector.com/en/view/b62472d11e1b67f172ed30289379b739");
        //        urlList.Add("https://replays.wotinspector.com/en/view/69b0487dddce62b29249c0af3988cbaa");
        //        urlList.Add("https://replays.wotinspector.com/en/view/ef9627f76e66918c9bc5a27e95c091fa");
        //        urlList.Add("https://replays.wotinspector.com/en/view/9dcc5ff54c4984526e58b162c9cc9ef5");
        //        urlList.Add("https://replays.wotinspector.com/en/view/26abe67800f47b65b9ffdcf6c1a5a64f");
        //        urlList.Add("https://replays.wotinspector.com/en/view/ea571fe25d334eb89e28115b2e5d5d31");
        //        urlList.Add("https://replays.wotinspector.com/en/view/b631f552ef98368d3ced057cf99ca9f4");
        //        urlList.Add("https://replays.wotinspector.com/en/view/332a5b36ce340c420f7ae672910a21e8");
        //        urlList.Add("https://replays.wotinspector.com/en/view/e4614acaa062e60fff85d123df4c121b");
        //        urlList.Add("https://replays.wotinspector.com/en/view/6095a2b691358f4c3faa2e2828fefe6e");
        //        urlList.Add("https://replays.wotinspector.com/en/view/a898549be76f1cc40e7c6602fa079ee6");
        //    }
        //    else if (tier == 11)
        //    {
        //        var lijnen = File.ReadAllLines(Bot.logInputPath);
        //        foreach (string line in lijnen)
        //        {
        //            urlList.Add(line);
        //        }
        //    }

        //    //urlList.Add("https://replays.wotinspector.com/en/view/d2a590b0ad827ca1e5ee2fcab315a892");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/a1fb4c2aec669878719865b03b2b9b1b");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/237619b9c4a0093976a2858db2561246");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/16849be245a1c3d75351381db8ff7291");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/68d76911d6fa1d5205d0bde096bc9a8b");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/69162fe04e23029de3b2a86d944a2a16");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/215a2548d4a0ee86e9ddc5a7151264ff");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/fb2c357aa28ef4fa30fe6ed331599a20");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/b721a15a36ae49a1d42fd8f6e58dd415");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/81c27125ba0d399e412721d2ed418307");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/5060b90f67efd05e6d8d0bd4454d5363");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/d7db39e5fca9a1ed4274d8065d5be2d8");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/3f970e9fa1cd1be5ad812a8fbb59126b");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/9a3efab31424e3a38254246465abaa09");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/753526b425ba2785e5ae9db164df3be5");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/1f0368838cd4b8861060f9263246a707");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/816464182148045e15000d0ec221813d");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/f47044fbbc94ca92cd63de2e7327cfb7");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/a3e7c315bd89b50d3d2071e3645a386f");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/14650a7e4407102225115d8e04e10001");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/ccbda08c10e1c3f10bbdd112499f466c");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/2125c7338f40209dc28ad222e1b7a0a8");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/ad6b606a57f561061d6727abef4939a1");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/f93712e374520c0a8cb0f3b5a665ee79");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/39075a110de11dda84ccd9f2cf106bd8");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/14ed2299fc58dd7b4f49cf53039e394b");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/9d81c7778365fcc2402be5d1136ff48e");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/f5c569cd103fe5ccce36c7394ea06b05");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/a046fd0a78f8f69627ea24511dc1e21a");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/5b389bc33a49d1a12c7824ae97c4df05");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/8e62f157b16c811bee9ec20eea33f0db");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/3fc24f2cf010c9692d028a03b45a5d22");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/52e271b50fb2033aa269a39e869037a0");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/d5408503c5ccb080afdb57f6a3a380db");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/f2a3abe84f2b9e782cbcd05e88af2b3b");
        //    //urlList.Add("https://replays.wotinspector.com/en/view/a9b5054beb468709eceea1316d46e9e0");

        //    //Verwijder diegene die al in de HOF staan
        //    DiscordChannel channel = await Bot.GetHallOfFameChannel(ctx.Guild.Id);
        //    IReadOnlyList<DiscordMessage> messages = await channel.GetMessagesAsync(100);
        //    List<Tuple<string, List<TankHof>>> tupleListWithTankHofs = new List<Tuple<string, List<TankHof>>>();
        //    for (int i = 1; i <= 10; i++)
        //    {
        //        List<DiscordMessage> tierMessages = Bot.getTierMessages(i, messages);
        //        if (tierMessages != null)
        //        {
        //            foreach (DiscordMessage message in tierMessages)
        //            {
        //                List<Tuple<string, List<TankHof>>> tempList = Bot.convertHOFMessageToTupleListAsync(message, i);
        //                if (tempList != null)
        //                {
        //                    tupleListWithTankHofs.AddRange(tempList);
        //                }
        //            }
        //        }
        //    }
        //    foreach (Tuple<string, List<TankHof>> tuple in tupleListWithTankHofs)
        //    {
        //        foreach (TankHof tankHof in tuple.Item2)
        //        {
        //            if (urlList.Contains(tankHof.link))
        //            {
        //                urlList.Remove(tankHof.link);
        //            }
        //        }
        //    }

        //    //Zet ze in de HOF
        //    await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Gaat " + urlList.Count + " replays testen.**");
        //    for (int i = 0; i < urlList.Count; i++)
        //    {
        //        await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "Link " + (i + 1) + ": `" + urlList[i] + "`");
        //        Tuple<string, DiscordMessage> returnedTuple = Bot.handle(string.Empty, ctx.Channel, ctx.Member, ctx.Guild.Name, ctx.Guild.Id, urlList[i]).Result;
        //        //if (returnedTuple.Item1.Equals(string.Empty))
        //        //{
        //        //    await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "Het is gelukt!");
        //        //}
        //        //else
        //        //{
        //        //    await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, returnedTuple.Item1);
        //        //}
        //    }
        //    await Bot.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**" + urlList.Count + " replays zijn getest.**");
        //    await Bot.confirmCommandExecuted(ctx.Message);
        //}
        #endregion
        #region testMessage
        //[Command("tm")]
        //public async Task testMessage(CommandContext ctx)
        //{
        //    await Bot.confirmCommandExecuting(ctx.Message);
        //    //IReadOnlyList<DiscordMessage> messages = await ctx.Channel.GetMessagesAsync(2);

        //    //foreach (DiscordMessage message in messages)
        //    //{
        //    //    if (message.Embeds.Count > 0)
        //    //    {
        //    //        foreach (DiscordEmbed anEmbed in message.Embeds)
        //    //        {
        //    //            foreach (DiscordEmbedField field in anEmbed.Fields)
        //    //            {
        //    //                string[] splitted = field.Value.Split(Environment.NewLine);
        //    //            }
        //    //        }
        //    //    }
        //    //}

        //    DiscordEmbedBuilder newDiscEmbedBuilder = new DiscordEmbedBuilder();
        //    newDiscEmbedBuilder.Color = DiscordColor.Red;
        //    newDiscEmbedBuilder.Title = "Prettige feestdagen!";
        //    //newDiscEmbedBuilder.Description = "Dit is een testbeschrijving.";

        //    //newDiscEmbedBuilder.AddField("Titel 1", "1.1 Uitleg\n1.2 Uitleg\n1.3 Uitleg\n1.4 Uitleg\n[link](http://example.com)", false);
        //    //newDiscEmbedBuilder.AddField("Titel 2", "2.1 Uitleg\n2.2 Uitleg\n2.3 Uitleg\n2.4 Uitleg\n", false);
        //    //newDiscEmbedBuilder.AddField("Titel 3", "3.1 Uitleg\n3.2 Uitleg\n3.3 Uitleg\n3.4 Uitleg\n", false);
        //    //newDiscEmbedBuilder.AddField("Titel 4", "4.1 Uitleg\n4.2 Uitleg\n4.3 Uitleg\n4.4 Uitleg\n", false);
        //    //for (int i = 0; i < 20; i++)
        //    //{
        //    //    StringBuilder sb = new StringBuilder();
        //    //    for (int j = 0; j < 5; j++)
        //    //    {
        //    //        sb.Append('\n');
        //    //        for (int k = 0; k < 50; k++)
        //    //        {
        //    //            sb.Append('H');
        //    //        }
        //    //    }
        //    //    newDiscEmbedBuilder.AddField("Titel " + (i + 1), sb.ToString());
        //    //}
        //    //newDiscEmbedBuilder.ImageUrl = "https://tenor.com/view/merry-christmas-snow-happy-holidays-christmas-tree-gif-7345277";
        //    newDiscEmbedBuilder.ImageUrl = "https://i0.wp.com/hyperallergic-newspack.s3.amazonaws.com/uploads/2012/12/christmas-animated-gifs-06.gif?quality=100";
        //    //SAO
        //    //newDiscEmbedBuilder.ImageUrl = "https://www.google.com/url?sa=i&url=https%3A%2F%2Fgifer.com%2Fen%2FSFHs&psig=AOvVaw39TqYc__Z9zXxOvH9ID36P&ust=1640524344469000&source=images&cd=vfe&ved=0CAsQjRxqFwoTCMiry9uD__QCFQAAAAAdAAAAABAD";
        //    DiscordEmbed embed = newDiscEmbedBuilder.Build();

        //    try
        //    {
        //        await ctx.Channel.SendMessageAsync(string.Empty, false, embed);
        //    }
        //    catch (Exception e)
        //    {
        //        await ctx.Channel.SendMessageAsync("**Er ging iets mis:\n" + e.Message + "**", false, null);
        //    }
        //    await Bot.confirmCommandExecuted(ctx.Message);
        //}
        #endregion
        #region GenerateTeams
        //[Command("GenerateTeams")]
        //[Aliases("gt", "gen", "gene", "gener", "genera", "generat", "generate", "generatet", "generatete", "generatetea", "generateteam")]
        //[Description("Genereert de teams voor het gegeven toernooi." +
        //    "Bijvoorbeeld:`" + Bot.Prefix + "gn` --> genereert de teams voor het meest recente bericht in Toernooi-aanmelden\n`" + Bot.Prefix + "gn 1` --> genereert de teams voor het meest recente bericht in Toernooi-aanmelden\n`" + Bot.Prefix + "gn 2` --> genereert de teams voor het 2de meest recente bericht in Toernooi-aanmelden")]
        //public async Task generateTeams(CommandContext ctx, params string[] optioneel_hoeveelste_toernooi_startende_vanaf_1_wat_de_recentste_voorstelt)
        //{
        //await Bot.confirmCommandExecuting(ctx.Message);
        //    if (!Bot.ignoreCommands)
        //    {
        //        if (Bot.hasRight(ctx.Member, ctx.Command))
        //        {
        //            List<Tier> tiers = await Bot.readTeams(ctx, optioneel_hoeveelste_toernooi_startende_vanaf_1_wat_de_recentste_voorstelt);
        //            if (tiers != null)
        //            {

        //                //Zoek het aantal verschillende deelnemers
        //                List<string> participants = Bot.getIndividualParticipants(tiers);

        //                //Check voor elke tier of er minstens 7 personen zijn dat willen meedoen
        //                List<KeyValuePair<bool, Tier>> checkedTiers = new List<KeyValuePair<bool, Tier>>();
        //                foreach(Tier aTier in tiers)
        //                {
        //                    if (aTier.Deelnemers.Count >= 7)
        //                    {
        //                        checkedTiers.Add(new KeyValuePair<bool, Tier>(true, aTier));
        //                    }
        //                    else
        //                    {
        //                        checkedTiers.Add(new KeyValuePair<bool, Tier>(false, aTier));
        //                    }
        //                }

        //                //Sorteer checkedTiers op aantal unieke deelnemers (dalend)
        //                List<KeyValuePair<bool, Tier>> sortedTiers = checkedTiers.OrderBy(p => p.Value.uniekelingen.Count).ToList();

        //                List<GenerateTeam> generatedTeams = new List<GenerateTeam>();
        //                bool firstTime = true;
        //                List<string> usedParticipants = new List<string>();
        //                foreach (var tier in sortedTiers)
        //                {
        //                    if (tier.Key)
        //                    {
        //                        GenerateTeam newTeam = new GenerateTeam(tier.Value.tier);
        //                        if (firstTime)
        //                        {
        //                            newTeam.leader = tier.Value.Organisator;
        //                            firstTime = false;
        //                        }
        //                        newTeam.deelnemers.AddRange(tier.Value.uniekelingen);


        //                        //Voeg spelers toe aan generatedTeam wanneer de speler maar in 1 van de tiers, die gaat spelen, mee kan doen
        //                        foreach (string deelnemer in tier.Value.Deelnemers)
        //                        {
        //                            if (!tier.Value.uniekelingen.Contains(deelnemer))
        //                            {
        //                                foreach (var xTier in sortedTiers)
        //                                {
        //                                    if (xTier.Key)
        //                                    {
        //                                        if (!xTier.Equals(tier))
        //                                        {
        //                                            if (!xTier.Value.Deelnemers.Contains(deelnemer))
        //                                            {
        //                                                if (newTeam.deelnemers.Count < 7)
        //                                                {
        //                                                    if (!usedParticipants.Contains(deelnemer))
        //                                                    {
        //                                                        newTeam.deelnemers.Add(deelnemer);
        //                                                        usedParticipants.Add(deelnemer);
        //                                                    }
        //                                                }
        //                                                else
        //                                                {
        //                                                    if (!usedParticipants.Contains(deelnemer))
        //                                                    {
        //                                                        newTeam.reserves.Add(deelnemer);
        //                                                        usedParticipants.Add(deelnemer);
        //                                                    }
        //                                                }
        //                                            }
        //                                        }
        //                                    }
        //                                }
        //                            }
        //                        }


        //                        //
        //                        //foreach (string deelnemer in tier.Value.Deelnemers)
        //                        //{
        //                        //    if (!usedParticipants.Contains(deelnemer))
        //                        //    {
        //                        //        bool 
        //                        //    }
        //                        //}

        //                        generatedTeams.Add(newTeam);
        //                    }
        //                }

        //                generatedTeams = generatedTeams.OrderBy(p => p.tier).ToList();

        //                List<DEF> deflist = new List<DEF>();
        //                foreach(GenerateTeam xTeam in generatedTeams)
        //                {
        //                    StringBuilder sb = new StringBuilder();
        //                    foreach(string deelnemer in xTeam.deelnemers)
        //                    {
        //                        sb.AppendLine(deelnemer);
        //                    }
        //                    DEF newdef = new DEF();
        //                    newdef.Inline = true;
        //                    newdef.Name = "Tier " + Emoj.getName(xTeam.tier);
        //                    newdef.Value = "Leider:\n" + xTeam.leader + "\n\n" + sb.ToString();
        //                    deflist.Add(newdef);
        //                }
        //                await Bot.CreateEmbed(ctx.Channel, string.Empty, string.Empty, "Gegenereerde Teams", (generatedTeams.Count > 0 ? "Organisator: " + tiers[0].Organisator : "Geen teams"), deflist, null, string.Empty, null);
        //            }
        //        }
        //        else
        //        {
        //            await Bot.SayTheUserIsNotAllowed(ctx.Channel);
        //        }
        //    }
        //await Bot.confirmCommandExecuted(ctx.Message);
        //}
        #endregion
        #region date
        //[Command("hofamount")]
        //public async Task testcommand(CommandContext ctx)
        //{
        //    if (!Bot.ignoreCommands)
        //    {
        //        if (Bot.hasRight(ctx.Member, ctx.Command))
        //        {
        //            DiscordChannel channel = await Bot.GetHallOfFameChannel(ctx.Guild.Id);
        //            if (channel != null)
        //            {
        //                IReadOnlyList<DiscordMessage> messages = await channel.GetMessagesAsync(100);
        //                for (short tier = 1; tier <= 10; tier++)
        //                {
        //                    List<DiscordMessage> tierMessages = Bot.getTierMessages(tier, messages);
        //                    foreach (DiscordMessage discordMessage in tierMessages)
        //                    {
        //                        List<Tuple<string, List<TankHof>>> discordMessageAsTankHofList = Bot.convertHOFMessageToTupleListAsync(discordMessage, tier);
        //                        if (discordMessageAsTankHofList != null)
        //                        {
        //                            //make the amount of replays per tank the HOF_AMOUNT_PER_TANK
        //                            for (int i = 0; i < discordMessageAsTankHofList.Count; i++)
        //                            {
        //                                discordMessageAsTankHofList[i] = new Tuple<string, List<TankHof>>(discordMessageAsTankHofList[i].Item1, discordMessageAsTankHofList[i].Item2.Take(Bot.HOF_AMOUNT_PER_TANK).ToList());
        //                            }

        //                            //edit the hof message
        //                            await Bot.editHOFMessage(discordMessage, discordMessageAsTankHofList);
        //                        }
        //                    }
        //                }
        //            }
        //            await ctx.Channel.SendMessageAsync("**HOF is aangepast naar " + Bot.HOF_AMOUNT_PER_TANK + " replays per tank.**");
        //            await Bot.confirmCommandExecuted(ctx.Message);
        //        }
        //        else
        //        {
        //            await Bot.SayTheUserIsNotAllowed(ctx.Channel);
        //        }
        //    }
        //}
        #endregion
        #region date
        //[Command("date")]
        //[Description("Geeft de datum.")]
        //public async Task testcommand(CommandContext ctx)
        //{
        //await Bot.confirmCommandExecuting(ctx.Message);
        //    StringBuilder sb = new StringBuilder();
        //    sb.AppendLine("TimeStamp.ToString(): " + ctx.Message.Timestamp.ToString());
        //    sb.AppendLine("TimeStamp.ToLocalTime(): " + ctx.Message.Timestamp.ToLocalTime());
        //    sb.AppendLine("Timestamp.LocalDateTime: " + ctx.Message.Timestamp.LocalDateTime);
        //    sb.AppendLine("DateTimeOffset: " + ctx.Message.CreationTimestamp);
        //    sb.AppendLine("DateTimeOffset.UtcDateTime: " + ctx.Message.CreationTimestamp.UtcDateTime);
        //    sb.AppendLine("DateTimeOffset.DateTime: " + ctx.Message.CreationTimestamp.DateTime);
        //    sb.AppendLine("DateTimeOffset.ToUniversalTime(): " + ctx.Message.CreationTimestamp.ToUniversalTime());
        //    sb.AppendLine("DateTimeOffset.LocalDateTime: " + ctx.Message.CreationTimestamp.LocalDateTime);
        //    sb.AppendLine("DateTimeOffset.Offset: " + ctx.Message.CreationTimestamp.Offset);
        //    sb.AppendLine("DateTimeOffset.ToOffset(): ctx.Message.CreationTimestamp.ToOffset(hier een TimeStamp)");
        //    sb.AppendLine("DateTimeOffset.ToOADate(): " + ctx.Message.CreationTimestamp.UtcDateTime.ToOADate());
        //    await Bot.CreateEmbed(ctx.Channel, string.Empty, string.Empty, "Alle manieren om een datum op te schrijven", sb.ToString(), null, null, string.Empty, null);
        //await Bot.confirmCommandExecuted(ctx.Message);
        //}
        #endregion
    }
}

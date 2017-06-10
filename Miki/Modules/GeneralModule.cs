﻿using IA;
using IA.Events;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using Miki.Core;
using Miki.Languages;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules
{
    class GeneralModule
    {
        public async Task LoadEvents(Bot bot)
        {
            await new RuntimeModule(module =>
            {
                module.Name = "General";
                module.Events = new List<ICommandEvent>()
                {
                    new CommandEvent(x =>
                        {
                            x.Name = "avatar";
                            x.Aliases = new string[] {"av"};
                            x.ProcessCommand = async (e, args) =>
                            {
                                if(args.StartsWith("-s"))
                                {
                                    await e.Channel.SendMessage(string.Join(".", e.Guild.AvatarUrl));
                                }
                                else if(e.MentionedUserIds.Count > 0)
                                { 
                                    await e.Channel.SendMessage(string.Join(".", (await e.Guild.GetUserAsync(e.MentionedUserIds.First())).AvatarUrl));
                                }
                                else
                                {
                                    await e.Channel.SendMessage(string.Join(".", e.Author.AvatarUrl));
                                }
                            };
                        }),
                    new CommandEvent(x =>
                        {
                            x.Name = "whois";
                            x.ProcessCommand = async (e, args) =>
                            {
                                ulong id = 0;

                                if(string.IsNullOrEmpty(args))
                                {
                                    id = e.Author.Id;
                                }
                                else if(e.MentionedUserIds.Count == 0)
                                {
                                    id = ulong.Parse(args);
                                }
                                else
                                {
                                    id = e.MentionedUserIds.First();
                                }

                                IDiscordUser user = await e.Guild.GetUserAsync(id);

                                Locale l = Locale.GetEntity(e.Channel.Id.ToDbLong());

                                IDiscordEmbed embed = e.CreateEmbed();
                                embed.Title = $"Who is {(string.IsNullOrEmpty(user.Nickname)? user.Username : user.Nickname)}!?";
                                embed.Color = new Color(0.5f, 0, 1);

                                embed.ImageUrl = (await e.Guild.GetUserAsync(id)).AvatarUrl;

                                embed.AddInlineField(
                                    l.GetString("miki_module_whois_tag_personal"),
                                    $"User Id      : **{user.Id}**\nUsername: **{user.Username}#{user.Discriminator} {(string.IsNullOrEmpty(user.Nickname)? "" : $"({user.Nickname})")}**\nCreated at: **{user.CreatedAt.ToString()}**\nJoined at   : **{user.JoinedAt.ToString()}**\n");
                                
                                List<string> roles = new List<string>();
                                foreach(ulong i in user.RoleIds)
                                {
                                    roles.Add("`" + user.Guild.GetRole(i).Name + "`");
                                }

                                embed.AddInlineField(
                                    l.GetString("miki_module_general_guildinfo_roles"), 
                                    string.Join(" ", roles));

                                await e.Channel.SendMessage(embed);
                            };
                        }),
                    new CommandEvent(x =>
                        {
                              x.Name = "calc";
                              x.ProcessCommand = async (e, args) =>
                              {
                                  Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

                                  try
                                  {
                                    var result = new DataTable().Compute(args, null);
                                    await e.Channel.SendMessage(result.ToString());
                                  }
                                  catch
                                  {
                                      await e.Channel.SendMessage(locale.GetString("miki_module_general_calc_error"));
                                  }
                              };
                        }),
                    new CommandEvent(x =>
                        {
                            x.Name = "guildinfo";
                            x.ProcessCommand = async (e, args) =>
                            {
                                IDiscordEmbed embed = e.CreateEmbed();
                                Locale l = Locale.GetEntity(e.Channel.Id.ToDbLong());

                                embed.SetAuthor(e.Guild.Name, e.Guild.AvatarUrl, e.Guild.AvatarUrl);

                                embed.AddInlineField(
                                    "👑" + l.GetString("miki_module_general_guildinfo_owned_by"),
                                    e.Guild.Owner.Username + "#" + e.Guild.Owner.Discriminator);

                                embed.AddInlineField(
                                    "👉" + l.GetString("miki_label_prefix"),
                                    await PrefixInstance.Default.GetForGuildAsync(e.Guild.Id));

                                embed.AddInlineField(
                                    "📺" + l.GetString("miki_module_general_guildinfo_channels"),
                                    e.Guild.ChannelCount.ToString());

                                embed.AddInlineField(
                                    "🔊" + l.GetString("miki_module_general_guildinfo_voicechannels"),
                                    e.Guild.VoiceChannelCount.ToString());

                                embed.AddInlineField(
                                    "🙎" + l.GetString("miki_module_general_guildinfo_users"),
                                    e.Guild.UserCount.ToString());

                                List<string> roleNames = new List<string>();
                                foreach(IDiscordRole r in e.Guild.Roles)
                                {
                                    roleNames.Add($"`{r.Name}`");
                                }

                                embed.AddInlineField(
                                    "#⃣" + l.GetString("miki_module_general_guildinfo_roles_count"),
                                    e.Guild.Roles.Count.ToString());

                                embed.AddInlineField(
                                    "📜" + l.GetString("miki_module_general_guildinfo_roles"),
                                    string.Join(", ", roleNames));

                                await e.Channel.SendMessage(embed);
                            };
                        }),
                    new CommandEvent(x =>
                        {
                            x.Name = "help";
                            x.Aliases = new string[]
                            {
                                 "commands", "command"
                            };
                            x.Cooldown = 2;
                            x.ProcessCommand = async (e, args) =>
                            {
                                Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

                                if (!string.IsNullOrEmpty(args))
                                {
                                    ICommandEvent ev = bot.Events.GetCommandEvent(args);
                                    if (ev == null)
                                    {
                                        IDiscordEmbed helpListEmbed = e.CreateEmbed();
                                        helpListEmbed.Title = locale.GetString("miki_module_help_error_null_header");
                                        helpListEmbed.Description = locale.GetString("miki_module_help_error_null_message");
                                        helpListEmbed.Color = new Color(1.0f, 0, 0);

                                        bool done = false;

                                        foreach(IModule a in bot.Events.Modules.Values)
                                        {
                                            foreach(ICommandEvent c in a.Events)
                                            {
                                                if(bot.Events.GetUserAccessibility(e) < c.Accessibility)
                                                {
                                                    continue;
                                                }

                                                if(done)
                                                {
                                                    break;
                                                }

                                                if(c.Name.Contains(args))
                                                {
                                                    helpListEmbed.AddField(f =>
                                                    {
                                                        f.Name = locale.GetString("miki_module_help_didyoumean");
                                                        f.Value = c.Name;
                                                    });
                                                    done=true;
                                                    break;
                                                }

                                                foreach(string alias in c.Aliases)
                                                {
                                                    if(alias.Contains(args))
                                                    {
                                                        helpListEmbed.AddField(f =>
                                                        {
                                                            f.Name = locale.GetString("miki_module_help_didyoumean");
                                                            f.Value = c.Name;
                                                        });
                                                        done=true;
                                                        break;
                                                    }
                                                }
                                            }

                                            if(done)
                                            {
                                                break;
                                            }
                                        }

                                        await e.Channel.SendMessage(helpListEmbed);
                                    }
                                    else
                                    {

                                        if(bot.Events.GetUserAccessibility(e) < ev.Accessibility)
                                        {
                                            return;
                                        }

                                        IDiscordEmbed explainedHelpEmbed = Utils.Embed()
                                            .SetTitle(ev.Name.ToUpper());

                                        if(ev.Aliases.Length > 0)
                                        {
                                            explainedHelpEmbed.AddInlineField(
                                                locale.GetString("miki_module_general_help_aliases"),
                                                string.Join(", ", ev.Aliases));
                                        }


                                        explainedHelpEmbed.AddField(
                                            locale.GetString("miki_module_general_help_description"),
                                            (locale.HasString("miki_command_description_" + ev.Name.ToLower())) ? locale.GetString("miki_command_description_" + ev.Name.ToLower()) : locale.GetString("miki_placeholder_null"));

                                        explainedHelpEmbed.AddField(
                                            locale.GetString("miki_module_general_help_usage"),
                                            (locale.HasString("miki_command_usage_" + ev.Name.ToLower())) ? locale.GetString("miki_command_usage_" + ev.Name.ToLower()) : locale.GetString("miki_placeholder_null"));

                                        await e.Channel.SendMessage(explainedHelpEmbed);
                                    }
                                    return;
                                }
                                IDiscordEmbed embed = e.CreateEmbed();

                                embed.Description = locale.GetString("miki_module_general_help_dm");

                                embed.Color = new Color(0, 0.5f, 1);

                                await e.Channel.SendMessage(embed);

                                await e.Author.SendMessage(await bot.Events.ListCommandsInEmbed(e));
                            };
                        }),
                    new CommandEvent(x =>
                        {
                          x.Name = "info";
                          x.Aliases = new string[]
                          {
                                "about"
                          };
                          x.Cooldown = 2;
                          x.ProcessCommand = async (e, args) =>
                          {
                              IDiscordEmbed embed = e.CreateEmbed();
                            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

                                embed.Author = embed.CreateAuthor();
                                embed.Author.Name = "Miki " + bot.Version;
                                embed.Color = new Color(1, 0.6f, 0.6f);


                              embed.AddField(f =>
                              {
                                  f.Name = locale.GetString("miki_module_general_info_made_by_header");
                                  f.Value = locale.GetString("miki_module_general_info_made_by_description");
                              });

                              embed.AddField(f =>
                              {
                                  f.Name = "Links";
                                  f.Value =
                                  $"**{locale.GetString("miki_module_general_info_docs")}:** https://www.github.com/velddev/miki/wiki \n" +
                                  $"**{locale.GetString("miki_module_general_info_patreon")}:** https://www.patreon.com/mikibot \n" +
                                  $"**{locale.GetString("miki_module_general_info_twitter")}:** https://www.twitter.com/velddev / https://www.twitter.com/miki_discord \n" +
                                  $"**{locale.GetString("miki_module_general_info_reddit")}:** https://www.reddit.com/r/mikibot \n" +
                                  $"**{locale.GetString("miki_module_general_info_server")}:** https://discord.gg/55sAjsW \n"+
                                  $"**{locale.GetString("miki_module_general_info_website")}:** http://miki.veld.one";
                              });

                              await e.Channel.SendMessage(embed);
                          };
                        }),
                    new CommandEvent(x =>
                        {
                             x.Name = "donate";
                             x.Aliases = new string[]
                             {
                                 "patreon"
                             };
                             x.ProcessCommand = async (e, args) =>
                             {
                                 Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());
                                 await e.Channel.SendMessage(locale.GetString("miki_module_general_info_donate_string") + " <https://www.patreon.com/mikibot>");
                             };
                        }),
                    new CommandEvent(x =>
                        {
                            x.Name = "ping";
                            x.Aliases = new string[]
                            {
                                  "lag"
                            };
                            x.ProcessCommand = async (e, args) =>
                            {
                                IDiscordMessage message = await e.Channel.SendMessage("Pong! ...");
                                if (message != null)
                                {
                                    await message.ModifyAsync("Pong! " + (message.Timestamp - e.Timestamp).TotalMilliseconds + "ms");
                                }
                            };
                        }),
                    new CommandEvent(x =>
                        {
                            x.Name = "prefix";
                            x.Accessibility = EventAccessibility.ADMINONLY;
                            x.ProcessCommand = async (e, args) =>
                            {
                                Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

                                if(string.IsNullOrEmpty(args))
                                {
                                    await e.Channel.SendMessage(Utils.ErrorEmbed(locale, locale.GetString("miki_module_general_prefix_error_no_arg")));
                                    return;
                                }
                                else if(args == "?")
                                {
                                    await Utils.Embed()
                                            .SetTitle(locale.GetString("miki_module_general_prefix_help_header"))
                                            .SetDescription(locale.GetString("miki_module_general_prefix_help", await PrefixInstance.Default.GetForGuildAsync(e.Guild.Id)))
                                            .SendToChannel(e.Channel.Id);
                                    return;
                                }

                                await PrefixInstance.Default.ChangeForGuildAsync(e.Guild.Id, args);

                                IDiscordEmbed embed = e.CreateEmbed();
                                embed.Title = locale.GetString("miki_module_general_prefix_success_header");
                                embed.Description = locale.GetString("miki_module_general_prefix_success_message", args);

                                embed.AddField(f =>
                                {
                                    f.Name = locale.GetString("miki_module_general_prefix_example_command_header");
                                    f.Value = $"{args}profile";
                                });

                                await e.Channel.SendMessage(embed);
                            };
                        }),
                    new CommandEvent(x =>
                        {
                            x.Name = "invite";
                            x.ProcessCommand = async (e, args) =>
                            {
                                Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());
                                Locale authorLocale = Locale.GetEntity(e.Author.Id.ToDbLong());
                                await e.Channel.SendMessage(locale.GetString("miki_module_general_invite_message"));
                                await e.Author.SendMessage(authorLocale.GetString("miki_module_general_invite_dm") + "\nhttps://discordapp.com/oauth2/authorize?&client_id=160185389313818624&scope=bot");
                            };
                        }),
                    new CommandEvent(x =>
                        {
                            x.Name = "urban";
                            x.ProcessCommand = async (e, args) =>
                            {
                                if (string.IsNullOrEmpty(args)) return;

                                args = args.Trim('.');

                                Locale l = Locale.GetEntity(e.Channel.Id.ToDbLong());
                                RestClient client = new RestClient("https://mashape-community-urban-dictionary.p.mashape.com/define?term=" + args);

                                RestRequest r = new RestRequest();
                                r.AddHeader("X-Mashape-Key", Global.UrbanKey);
                                r.AddHeader("Accept", "application/json");

                                RestResponse entry = (RestResponse)client.Execute(r);
                                UrbanDictionaryInformation post = JsonConvert.DeserializeObject<UrbanDictionaryInformation>(entry.Content);

                                IDiscordEmbed embed = e.CreateEmbed();
                                embed.Title = post.Entries[0].word;
                                embed.Description = l.GetString("miki_module_general_urban_author", post.Entries[0].author);
                                embed.AddField(f =>
                                {
                                    f.Name = l.GetString("miki_module_general_urban_definition");
                                    f.Value = post.Entries[0].definition;
                                    f.IsInline = true;
                                });
                                embed.AddField(f =>
                                {
                                    f.Name = l.GetString("miki_module_general_urban_example");
                                    f.Value = post.Entries[0].example;
                                    f.IsInline = true;
                                });
                                embed.AddField(f =>
                                {
                                    f.Name = l.GetString("miki_module_general_urban_rating");
                                    f.Value = "👍 " + post.Entries[0].thumbs_up + "  👎 " + post.Entries[0].thumbs_down;
                                    f.IsInline = true;
                                });
                                await e.Channel.SendMessage(embed);
                        };
                    })
                };
            }).InstallAsync(bot);
        }
    }
}
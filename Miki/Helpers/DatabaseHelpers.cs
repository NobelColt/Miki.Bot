﻿using Miki.Accounts.Achievements;
using Miki.Discord.Common;
using Miki.Exceptions;
using Miki.Models;
using StatsdClient;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Helpers
{
    public static class DatabaseHelpers
    {
		public static async Task<User> GetUserAsync(MikiContext context, IDiscordUser discordUser)
			=> await User.GetAsync(context, (long)discordUser.Id, discordUser.Username);

		public static async Task<Achievement> GetAchievementAsync(MikiContext context, long userId, string name)
		{
			string key = $"achievement:{userId}:{name}";

			if (await Global.RedisClient.ExistsAsync(key))
			{
				Achievement a = await Global.RedisClient.GetAsync<Achievement>(key);
				if (a != null)
				{
					return context.Attach(a).Entity;
				}
			}

			Achievement achievement = await context.Achievements.FindAsync(userId, name);
			await Global.RedisClient.UpsertAsync(key, achievement);
			return achievement;
		}

		internal static async Task UpdateCacheAchievementAsync(long userId, string name, Achievement achievement)
		{
			string key = $"achievement:{userId}:{name}";
			await Global.RedisClient.UpsertAsync(key, achievement);
		}

		public static async Task AddCurrencyAsync(this User user, int amount, IDiscordChannel channel = null, User fromUser = null)
		{
			if (user.Banned) return;

			if (amount < 0)
			{
				if (user.Currency < Math.Abs(amount))
				{
					throw new InsufficientCurrencyException(user.Currency, Math.Abs(amount));
				}
			}

			DogStatsd.Counter("currency.change", amount);

			user.Currency += amount;

			if (channel is IDiscordGuildChannel guildchannel)
			{
				await AchievementManager.Instance.CallTransactionMadeEventAsync(guildchannel, user, fromUser, amount);
			}
		}
	}
}

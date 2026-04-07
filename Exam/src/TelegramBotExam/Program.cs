using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace TelegramBotExam;

public class Program
{
    private static readonly string BotToken =
        Environment.GetEnvironmentVariable("MY_BOT_TOKEN")!;
    private static readonly string DataFolder = "Data";
    private static readonly string UsersFilePath = Path.Combine(DataFolder, "users.json");

    static async Task Main(string[] args)
    {
        if (!Directory.Exists(DataFolder))
        {
            Directory.CreateDirectory(DataFolder);
        }

        if (!File.Exists(UsersFilePath))
        {
            await File.WriteAllTextAsync(UsersFilePath, "[]");
        }

        var botClient = new TelegramBotClient(BotToken);
        using var cts = new CancellationTokenSource();

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            cancellationToken: cts.Token
        );

        Console.WriteLine("Bot muvaffaqiyatli ishga tushdi...");
        Console.ReadLine();
        cts.Cancel();
    }

    static async Task HandleUpdateAsync(ITelegramBotClient botClient,
        Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { Text: { } messageText } message)
            return;

        var chatId = message.Chat.Id;
        var user = message.From;

        if (messageText.ToLower() == "/start")
        {
            var existingJson = await File.ReadAllTextAsync(UsersFilePath, cancellationToken);
            var usersList = JsonSerializer.Deserialize<List<BotUser>>(existingJson)
                ?? new List<BotUser>();

            if (!usersList.Any(u => u.Id == user!.Id))
            {
                usersList.Add(new BotUser
                {
                    Id = user!.Id,
                    FirstName = user.FirstName,
                    Username = user.Username,
                    DateAdded = DateTime.Now
                });

                var updatedJson = JsonSerializer.Serialize(usersList,
                    new JsonSerializerOptions { WriteIndented = true });

                await File.WriteAllTextAsync(UsersFilePath, updatedJson, cancellationToken);
            }

            var replyKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "Rasmlarni ko'rish", "Bot haqida" }
            })
            {
                ResizeKeyboard = true
            };

            await botClient.SendMessage(
                chatId: chatId,
                text: $"Salom, {user!.FirstName}! Sizning ma'lumotlaringiz bazaga saqlandi. Menga istalgan so'z yozing yoki tugmalardan birini bosing.",
                replyMarkup: replyKeyboard,
                cancellationToken: cancellationToken
            );
        }
        else
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "Rasmlar izlanmoqda... ⏳",
                cancellationToken: cancellationToken
            );

            var imageUrls = new[]
            {
                "https://picsum.photos/800/600?random=1",
                "https://picsum.photos/800/600?random=2",
                "https://picsum.photos/800/600?random=3"
            };

            var mediaGroup = imageUrls.Select(url => new InputMediaPhoto(InputFile.FromUri(url))).ToList();

            await botClient.SendMediaGroup(
                chatId: chatId,
                media: mediaGroup,
                cancellationToken: cancellationToken
            );

            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData("Zo'r", "like"),
                    InlineKeyboardButton.WithCallbackData("Yoqmadi", "dislike")
                }
            });

            await botClient.SendMessage(
                chatId: chatId,
                text: "Rasmlar yoqdimi? Yana xohlasangiz tugmalardan foydalaning yoki biror narsa yozing!",
                replyMarkup: inlineKeyboard,
                cancellationToken: cancellationToken
            );
        }
    }

    static Task HandleErrorAsync(ITelegramBotClient botClient,
        Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException =>
                $"Telegram API xatosi:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }
}
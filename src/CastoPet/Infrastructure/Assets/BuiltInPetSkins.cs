using CastoPet.Core.Animation;
using CastoPet.Core.Skins;

namespace CastoPet.Infrastructure.Assets;

public static class BuiltInPetSkins
{
    private const string Root = "Assets/Runtime/Castorice";

    public static readonly PetSkinDefinition Castorice = new(
        "castorice",
        "Castorice",
        Root,
        $"{Root}/Castorice.png",
        [
            new PetActionDefinition(
                "idle",
                PetActionKind.Idle,
                Enumerable.Range(0, 8)
                    .Select(index => $"{Root}/States/Idle/Castorice.Idle.{index:00}.png")
                    .ToArray(),
                FrameInterval: TimeSpan.FromMilliseconds(125)),
            new PetActionDefinition(
                "blink",
                PetActionKind.Blink,
                Enumerable.Range(0, 5)
                    .Select(index => $"{Root}/States/Blink/Castorice.Blink.{index:00}.png")
                    .ToArray(),
                FrameInterval: TimeSpan.FromMilliseconds(45),
                MinScheduleDelay: TimeSpan.FromSeconds(3),
                MaxScheduleDelay: TimeSpan.FromSeconds(7),
                FrameDurations:
                [
                    TimeSpan.FromMilliseconds(40),
                    TimeSpan.FromMilliseconds(45),
                    TimeSpan.FromMilliseconds(60),
                    TimeSpan.FromMilliseconds(45),
                    TimeSpan.FromMilliseconds(35),
                ]),
        ]);
}

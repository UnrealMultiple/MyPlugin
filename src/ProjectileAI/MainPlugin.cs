﻿using TerrariaApi.Server;

using MonoMod.RuntimeDetour;
using Org.BouncyCastle.Asn1.X509;

namespace VBY.ProjectileAI;

[ApiVersion(2, 1)]
public class ProjectileAIPlugin : TerrariaPlugin
{
    public override string Name => "ProjectileAIPlugin Base";
    public override string Author => "yu";
    private static readonly Detour AIDetour = new(typeof(Projectile).GetMethod("AI"), typeof(AIs).GetMethod("AI"), new() { ManualApply = true });
    public ProjectileAIPlugin(Main game) : base(game)
    {
    }
    public override void Initialize()
    {
        AIDetour.Apply();
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            AIDetour.Dispose();
        }
        base.Dispose(disposing);
    }
}
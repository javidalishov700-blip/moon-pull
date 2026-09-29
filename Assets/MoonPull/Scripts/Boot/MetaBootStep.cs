using System.Collections;
using MoonPull.Core;
using MoonPull.Core.Boot;
using MoonPull.Meta;
using MoonPull.Save;
using UnityEngine;

namespace MoonPull.Boot
{
    public sealed class MetaBootStep : BootStep
    {
        [SerializeField] private MetaGame meta;

        public override IEnumerator Run()
        {
            meta.Initialize(Services.Get<ISaveService>(), Services.Get<IClock>());
            yield break;
        }
    }
}

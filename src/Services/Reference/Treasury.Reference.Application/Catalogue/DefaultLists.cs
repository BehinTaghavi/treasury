using Treasury.Reference.Domain.Catalogue;

namespace Treasury.Reference.Application.Catalogue;

/// <summary>
/// Starting content of the Basic Information lists for a new client database, so the forms are usable on day one.
/// Added through the normal use cases (same validation and events as an entry typed by a user), only for a list that is
/// still completely empty; afterwards the client edits, adds or deactivates entries on the Basic Information screen.
/// Old clients' own lists replace this through the migration tool.
/// </summary>
public static class DefaultLists
{
    public static readonly IReadOnlyList<(string Code, string Name)> Currencies =
    [
        ("IRR", "ریال"), ("USD", "دلار آمریکا"), ("EUR", "یورو"), ("AED", "درهم امارات"),
        ("CNY", "یوان چین"), ("GBP", "پوند انگلیس"), ("TRY", "لیر ترکیه"), ("RUB", "روبل روسیه"),
    ];

    /// <summary>Iran's 31 provinces and the capital of each.</summary>
    public static readonly IReadOnlyList<(string Province, string Capital)> IranProvinces =
    [
        ("آذربایجان شرقی", "تبریز"), ("آذربایجان غربی", "ارومیه"), ("اردبیل", "اردبیل"), ("اصفهان", "اصفهان"),
        ("البرز", "کرج"), ("ایلام", "ایلام"), ("بوشهر", "بوشهر"), ("تهران", "تهران"),
        ("چهارمحال و بختیاری", "شهرکرد"), ("خراسان جنوبی", "بیرجند"), ("خراسان رضوی", "مشهد"), ("خراسان شمالی", "بجنورد"),
        ("خوزستان", "اهواز"), ("زنجان", "زنجان"), ("سمنان", "سمنان"), ("سیستان و بلوچستان", "زاهدان"),
        ("فارس", "شیراز"), ("قزوین", "قزوین"), ("قم", "قم"), ("کردستان", "سنندج"),
        ("کرمان", "کرمان"), ("کرمانشاه", "کرمانشاه"), ("کهگیلویه و بویراحمد", "یاسوج"), ("گلستان", "گرگان"),
        ("گیلان", "رشت"), ("لرستان", "خرم‌آباد"), ("مازندران", "ساری"), ("مرکزی", "اراک"),
        ("هرمزگان", "بندرعباس"), ("همدان", "همدان"), ("یزد", "یزد"),
    ];

    public static readonly IReadOnlyList<string> Banks =
    [
        "بانک ملی ایران", "بانک ملت", "بانک صادرات ایران", "بانک تجارت", "بانک سپه", "بانک کشاورزی", "بانک مسکن",
        "بانک رفاه کارگران", "پست بانک ایران", "بانک توسعه صادرات ایران", "بانک صنعت و معدن", "بانک توسعه تعاون",
        "بانک اقتصاد نوین", "بانک پارسیان", "بانک پاسارگاد", "بانک سامان", "بانک سینا", "بانک کارآفرین",
        "بانک خاورمیانه", "بانک شهر", "بانک دی", "بانک گردشگری", "بانک ایران زمین", "بانک سرمایه", "بانک آینده",
        "بانک قرض‌الحسنه مهر ایران", "بانک قرض‌الحسنه رسالت", "موسسه اعتباری ملل",
    ];

    /// <summary>Fills every list that has no entry at all (active or not). Safe to run on every start: idempotency keys are fixed.</summary>
    public static async Task SeedAsync(CatalogueUseCases useCases, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCases);

        async Task<bool> IsEmpty(CatalogueKind kind) =>
            (await useCases.ListAsync(kind, null, includeInactive: true, cancellationToken).ConfigureAwait(false)).Count == 0;

        async Task<Guid> Add(CatalogueKind kind, string name, Guid? parent = null, string? code = null)
        {
            var result = await useCases.AddAsync(
                new AddCatalogueItemCommand($"seed:{kind}:{parent}:{code}:{name}", kind, name, parent, code), cancellationToken).ConfigureAwait(false);
            return result.IsSuccess ? result.Value : throw new InvalidOperationException($"Default entry '{name}' was refused: {result.ErrorCode}");
        }

        if (await IsEmpty(CatalogueKind.Currency).ConfigureAwait(false))
        {
            foreach (var (code, name) in Currencies)
            {
                await Add(CatalogueKind.Currency, name, code: code).ConfigureAwait(false);
            }
        }

        if (await IsEmpty(CatalogueKind.Country).ConfigureAwait(false))
        {
            var iran = await Add(CatalogueKind.Country, "ایران").ConfigureAwait(false);
            foreach (var (province, capital) in IranProvinces)
            {
                var provinceId = await Add(CatalogueKind.Province, province, iran).ConfigureAwait(false);
                await Add(CatalogueKind.City, capital, provinceId).ConfigureAwait(false);
            }
        }

        if (await IsEmpty(CatalogueKind.Bank).ConfigureAwait(false))
        {
            foreach (var bank in Banks)
            {
                await Add(CatalogueKind.Bank, bank).ConfigureAwait(false);
            }
        }
    }
}

using Treasury.BuildingBlocks.Domain;
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

    /// <summary>The references (عطف) the new-version manual lists as fixed ones (item 8 "تعریف عطف های ثابت").</summary>
    public static readonly IReadOnlyList<string> References =
    [
        "شماره قرارداد", "شماره سفارش فروش", "شماره صورتحساب فروش", "شماره سفارش اسنادی",
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

        if (await IsEmpty(CatalogueKind.Reference).ConfigureAwait(false))
        {
            foreach (var reference in References)
            {
                await Add(CatalogueKind.Reference, reference).ConfigureAwait(false);
            }
        }

        await AddIranCitiesAsync(useCases, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Iran's cities by province (embedded IranCities.csv: 1,354 cities with their Statistical Centre code).</summary>
    public static IReadOnlyList<(string Province, string City, string Code)> IranCities { get; } = ReadIranCities();

    /// <summary>
    /// Adds the cities of <see cref="IranCities"/> that are missing under Iran's provinces (owner, 11 Nov 2026: "لطفا فهرست شهر های
    /// ایران را اضافه فرمایید"). Runs on every start and also fills databases created before the list existed. Each city is added
    /// with a fixed idempotency key, so a city the client deleted or renamed is never added again; a city already typed by the
    /// client (same name in the same province) is left as it is; a province or country the client renamed is skipped.
    /// </summary>
    public static async Task<int> AddIranCitiesAsync(CatalogueUseCases useCases, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCases);
        var iranKey = PersianText.Key("ایران");
        var iran = (await useCases.ListAsync(CatalogueKind.Country, null, includeInactive: true, cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(c => PersianText.Key(c.Name) == iranKey);
        if (iran is null)
        {
            return 0;
        }

        var provinces = (await useCases.ListAsync(CatalogueKind.Province, iran.Id, includeInactive: true, cancellationToken).ConfigureAwait(false))
            .GroupBy(p => PersianText.Key(p.Name))
            .ToDictionary(g => g.Key, g => g.First());
        var added = 0;
        foreach (var byProvince in IranCities.GroupBy(c => PersianText.Key(c.Province)))
        {
            if (!provinces.TryGetValue(byProvince.Key, out var province) || !province.IsActive)
            {
                continue;
            }

            var existing = (await useCases.ListAsync(CatalogueKind.City, province.Id, includeInactive: true, cancellationToken).ConfigureAwait(false))
                .Select(c => PersianText.Key(c.Name))
                .ToHashSet(StringComparer.Ordinal);
            var missing = byProvince.Where(c => !existing.Contains(PersianText.Key(c.City))).ToList();
            foreach (var (_, city, code) in missing)
            {
                // A key used before (city later deleted or renamed) only returns the old answer; any refusal leaves the city out.
                await useCases.AddAsync(
                    new AddCatalogueItemCommand($"seed:iran-city:{code}", CatalogueKind.City, city, province.Id), cancellationToken).ConfigureAwait(false);
            }

            if (missing.Count > 0)
            {
                added += (await useCases.ListAsync(CatalogueKind.City, province.Id, includeInactive: true, cancellationToken).ConfigureAwait(false)).Count - existing.Count;
            }
        }

        return added;
    }

    private static List<(string Province, string City, string Code)> ReadIranCities()
    {
        using var stream = typeof(DefaultLists).Assembly.GetManifestResourceStream("Treasury.Reference.IranCities.csv")
            ?? throw new InvalidOperationException("The embedded list of Iran's cities is missing.");
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
        var list = new List<(string, string, string)>();
        while (reader.ReadLine() is { } line)
        {
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith("province,", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = line.Split(',');
            list.Add((parts[0], parts[1], parts[2]));
        }

        return list;
    }
}

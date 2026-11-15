using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Domain.Catalogue;
using Xunit;

namespace Treasury.Reference.Domain.Tests;

/// <summary>Rules of the Basic Information lists (new-version manual, "اطلاعات پایه" items 1–4; MVC tables BasicBanks, City, ExchangeTypes).</summary>
public sealed class CatalogueItemTests
{
    private static readonly CatalogueItem Iran = CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Country, "ایران");

    [Fact]
    public void A_bank_name_is_added_active_and_raises_one_event()
    {
        var bank = CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Bank, "  بانک   ملت ");
        Assert.Equal("بانک ملت", bank.Name);
        Assert.True(bank.IsActive);
        Assert.Null(bank.ParentId);
        var e = Assert.IsType<CatalogueItemChanged>(Assert.Single(bank.PendingEvents));
        Assert.Equal(CatalogueKind.Bank, e.Kind);
    }

    [Fact]
    public void A_name_is_required_and_limited()
    {
        Assert.Equal("Catalogue.NameRequired", Assert.Throws<DomainException>(() => CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Bank, "  ")).Code);
        Assert.Equal("Catalogue.NameTooLong", Assert.Throws<DomainException>(() => CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Bank, new string('ب', 101))).Code);
    }

    [Fact]
    public void A_province_belongs_to_a_country_and_a_city_to_a_province()
    {
        // Manual: "استان تابعی است از کشور" / "شهر تابعی است از استان و کشور"
        var tehranProvince = CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Province, "تهران", Iran);
        var tehran = CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.City, "تهران", tehranProvince);
        Assert.Equal(Iran.Id, tehranProvince.ParentId);
        Assert.Equal(tehranProvince.Id, tehran.ParentId);

        Assert.Equal("Catalogue.ParentRequired", Assert.Throws<DomainException>(() => CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.City, "کرج")).Code);
        Assert.Equal("Catalogue.WrongParent", Assert.Throws<DomainException>(() => CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.City, "کرج", Iran)).Code);
        Assert.Equal("Catalogue.WrongParent", Assert.Throws<DomainException>(() => CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Bank, "ملت", Iran)).Code);
    }

    [Fact]
    public void A_city_can_move_to_another_province_of_the_right_kind_owner_item_6()
    {
        var isfahan = CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Province, "اصفهان", Iran);
        var tehranProvince = CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Province, "تهران", Iran);
        var city = CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.City, "اصفهان", isfahan);
        city.MarkSaved(1);

        city.Edit("تهران", tehranProvince);
        Assert.Equal(("تهران", tehranProvince.Id), (city.Name, city.ParentId));
        Assert.Equal(tehranProvince.Id, Assert.IsType<CatalogueItemChanged>(Assert.Single(city.PendingEvents)).ParentId);
        Assert.Equal("Catalogue.WrongParent", Assert.Throws<DomainException>(() => city.Edit("تهران", Iran)).Code);
        isfahan.Deactivate();
        Assert.Equal("Catalogue.ParentInactive", Assert.Throws<DomainException>(() => city.Edit("اصفهان", isfahan)).Code);
    }

    [Fact]
    public void A_currency_code_can_be_corrected_until_the_currency_is_used_owner_item_2()
    {
        var qatar = CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Currency, "ریال قطر", code: "QAR");
        qatar.Edit("دینار کویت", code: "kwd");
        Assert.Equal(("دینار کویت", "KWD"), (qatar.Name, qatar.Code));
        Assert.True(qatar.MarkInUse());
        Assert.False(qatar.MarkInUse());
        Assert.Equal("Catalogue.CodeInUse", Assert.Throws<DomainException>(() => qatar.Edit("دینار کویت", code: "QAR")).Code);
        qatar.Edit("دینار کویت", code: "KWD"); // the same code is not a change

        var rial = CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Currency, "ریال", code: "IRR");
        Assert.Equal("Catalogue.RialCodeFixed", Assert.Throws<DomainException>(() => rial.Edit("ریال", code: "IRT")).Code);
        Assert.Equal("Catalogue.CodeNotAllowed", Assert.Throws<DomainException>(() => Iran.Edit("ایران", code: "IRN")).Code);
    }

    [Fact]
    public void Only_an_unused_entry_without_entries_under_it_can_be_deleted_owner_items_6_and_9()
    {
        var bank = CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Bank, "بانک اشتباه");
        bank.MarkSaved(1);
        Assert.Equal("Catalogue.HasChildren", Assert.Throws<DomainException>(() => Iran.Delete(hasChildren: true)).Code);
        bank.Delete(hasChildren: false);
        Assert.IsType<CatalogueItemDeleted>(Assert.Single(bank.PendingEvents));

        var used = CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.City, "کرج", CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Province, "البرز", Iran));
        used.MarkInUse();
        Assert.Equal("Catalogue.InUse", Assert.Throws<DomainException>(() => used.Delete(hasChildren: false)).Code);
        var rial = CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Currency, "ریال", code: "IRR");
        Assert.Equal("Catalogue.RialCannotBeDeleted", Assert.Throws<DomainException>(() => rial.Delete(hasChildren: false)).Code);
    }

    [Theory]
    [InlineData("بانک آینده", "آینده")]
    [InlineData("بانك ملي ايران", "ملی ایران")]
    [InlineData("پست بانک ایران", "پست ایران")]
    public void Bank_names_are_compared_without_the_word_bank_owner_item_8(string a, string b) =>
        Assert.Equal(
            CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Bank, a).MatchKey,
            CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Bank, b).MatchKey);

    [Fact]
    public void Other_lists_keep_the_word_bank_in_the_comparison() =>
        Assert.NotEqual(
            CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Country, "بانک مرکزی").MatchKey,
            CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Country, "مرکزی").MatchKey);

    [Fact]
    public void Nothing_can_be_added_under_an_inactive_parent()
    {
        var province = CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Province, "البرز", Iran);
        province.Deactivate();
        Assert.Equal("Catalogue.ParentInactive", Assert.Throws<DomainException>(() => CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.City, "کرج", province)).Code);
    }

    [Theory]
    [InlineData("usd", "USD")]
    [InlineData(" EUR ", "EUR")]
    public void A_currency_has_a_three_letter_code_in_capitals(string typed, string stored) =>
        Assert.Equal(stored, CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Currency, "دلار", code: typed).Code);

    [Theory]
    [InlineData(null)]
    [InlineData("US")]
    [InlineData("دلا")]
    [InlineData("US1")]
    public void An_invalid_currency_code_is_refused(string? code) =>
        Assert.Equal("Catalogue.CurrencyCodeInvalid", Assert.Throws<DomainException>(() => CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Currency, "ارز", code: code)).Code);

    [Fact]
    public void Only_currencies_have_a_code() =>
        Assert.Equal("Catalogue.CodeNotAllowed", Assert.Throws<DomainException>(() => CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Bank, "ملت", code: "MEL")).Code);

    [Fact]
    public void The_rial_cannot_be_deactivated_but_other_currencies_can()
    {
        var rial = CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Currency, "ریال", code: "IRR");
        Assert.Equal("Catalogue.RialCannotBeDeactivated", Assert.Throws<DomainException>(rial.Deactivate).Code);
        var dollar = CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Currency, "دلار آمریکا", code: "USD");
        dollar.Deactivate();
        Assert.False(dollar.IsActive);
    }

    [Fact]
    public void Deactivating_and_activating_again_raises_an_event_each_time_and_repeats_are_ignored()
    {
        // Manual (bank accounts): "این عمل ممکن است به صورت موقتی انجام شود ... دوباره به حالت فعال درآید"
        var bank = CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Bank, "بانک سپه");
        bank.MarkSaved(1);
        bank.Deactivate();
        bank.Deactivate();
        bank.Activate();
        Assert.True(bank.IsActive);
        Assert.Equal(2, bank.PendingEvents.Count);
    }

    [Fact]
    public void Renaming_cleans_the_name_and_an_unchanged_name_raises_nothing()
    {
        var bank = CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Bank, "بانک تجارت");
        bank.MarkSaved(1);
        bank.Rename(" بانک   تجارت ");
        Assert.Empty(bank.PendingEvents);
        bank.Rename("بانک تجارت ایران");
        Assert.Equal("بانک تجارت ایران", bank.Name);
        Assert.Single(bank.PendingEvents);
    }

    [Theory]
    [InlineData("بانك ملي", "بانک ملی")]
    [InlineData("بانک‌ملی", "بانک ملی")]
    [InlineData("شعبه ۱۲", "شعبه 12")]
    [InlineData("شعبه ١٢", "شعبه 12")]
    [InlineData("Bank  MELLI", "bank melli")]
    public void Names_written_with_arabic_letters_or_other_digits_have_the_same_key(string a, string b) =>
        Assert.Equal(PersianText.Key(b), PersianText.Key(a));

    [Fact]
    public void Different_names_have_different_keys() =>
        Assert.NotEqual(PersianText.Key("بانک ملی"), PersianText.Key("بانک ملت"));
}

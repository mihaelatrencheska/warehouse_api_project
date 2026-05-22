using BoutiqueInventory.Application.Interfaces;
using BoutiqueInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BoutiqueInventory.Infrastructure.Data;

/// <summary>
/// Applies pending migrations and seeds a realistic boutique dataset so the
/// API is usable out of the box.
/// </summary>
public static class DbSeeder
{
    public static async Task MigrateAndSeedAsync(
        AppDbContext db,
        IProductSearchIndex searchIndex,
        CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);
        await searchIndex.EnsureSchemaAsync(ct);

        if (await db.Warehouses.AnyAsync(ct))
        {
            if (await searchIndex.IsEmptyAsync(ct))
            {
                await searchIndex.RebuildAsync(ct);
            }

            return;
        }

        // ── Categories ────────────────────────────────────────────────────
        var catPerfume   = new Category { Name = "Perfume",         Description = "Eau de Parfum fragrances" };
        var catEdt       = new Category { Name = "Eau de Toilette", Description = "Eau de Toilette fragrances" };
        var catSkincare  = new Category { Name = "Skincare",        Description = "Skincare products" };
        var catHandbag   = new Category { Name = "Handbag",         Description = "Designer handbags" };
        var catShoes     = new Category { Name = "Shoes",           Description = "Designer footwear" };
        var catMakeup    = new Category { Name = "Makeup",          Description = "Makeup and cosmetics" };
        var catAccessory = new Category { Name = "Accessories",     Description = "Scarves, jewelry, belts" };
        db.Categories.AddRange(catPerfume, catEdt, catSkincare, catHandbag, catShoes, catMakeup, catAccessory);

        // ── Warehouses + sections ─────────────────────────────────────────
        static (Warehouse wh, WarehouseSection perfSection, WarehouseSection accSection, WarehouseSection skincareSection, WarehouseSection bagsSection)
            MakeWarehouse(string name, string location, bool isActive = true)
        {
            var wh = new Warehouse { Name = name, Location = location, IsActive = isActive, DeactivatedAt = isActive ? null : DateTimeOffset.UtcNow };
            var perf     = new WarehouseSection { Warehouse = wh, Name = "Perfumes" };
            var acc      = new WarehouseSection { Warehouse = wh, Name = "Accessories" };
            var skincare = new WarehouseSection { Warehouse = wh, Name = "Skincare & Makeup" };
            var bags     = new WarehouseSection { Warehouse = wh, Name = "Bags & Shoes" };
            wh.Sections.Add(perf);
            wh.Sections.Add(acc);
            wh.Sections.Add(skincare);
            wh.Sections.Add(bags);
            return (wh, perf, acc, skincare, bags);
        }

        var (skopje,   skopjePerf,   skopjeAcc,   skopjeSkincare,   skopjeBags)   = MakeWarehouse("Skopje Main Store", "Skopje");
        var (bitola,   bitolaPerf,   bitolaAcc,   bitolaSkincare,   bitolaBags)   = MakeWarehouse("Bitola Branch",     "Bitola");
        var (strumica, strumicaPerf, strumicaAcc, strumicaSkincare, strumicaBags) = MakeWarehouse("Strumica Branch",   "Strumica");
        var (ohrid,    ohridPerf,    ohridAcc,    ohridSkincare,    ohridBags)    = MakeWarehouse("Ohrid Seasonal",    "Ohrid");
        var (veles,    velesPerf,    velesAcc,    velesSkincare,    velesBags)    = MakeWarehouse("Veles Storage",     "Veles", isActive: false);

        db.Warehouses.AddRange(skopje, bitola, strumica, ohrid, veles);
        await db.SaveChangesAsync(ct);

        // ── Helper ────────────────────────────────────────────────────────
        Product MakeProduct(
            string name, string sku, string size, string type,
            WarehouseSection section, Category category,
            DateTimeOffset? expiry = null)
        {
            var p = new Product
            {
                Name = name, Sku = sku, Size = size, Type = type,
                WarehouseSectionId = section.Id,
                ExpirationDate = expiry
            };
            p.Categories.Add(new ProductCategory { Product = p, CategoryId = category.Id });
            return p;
        }

        var now = DateTimeOffset.UtcNow;

        // ── Perfume — 20 products ─────────────────────────────────────────
        var perfumes = new[]
        {
            // ---- existing ----
            MakeProduct("Sauvage EDP",                  "FRAG-DIOR-SAUV-100",  "100ml", "Eau de Parfum", skopjePerf,   catPerfume, now.AddDays(10)),
            MakeProduct("Bleu de Chanel EDP",           "FRAG-CHAN-BLEU-100",  "100ml", "Eau de Parfum", bitolaPerf,   catPerfume, now.AddDays(7)),
            MakeProduct("Chanel No. 5",                 "FRAG-CHAN-NO5-50",    "50ml",  "Eau de Parfum", ohridPerf,    catPerfume, now.AddDays(25)),
            MakeProduct("Terre d'Hermès EDP",           "FRAG-HERM-TERR-75",  "75ml",  "Eau de Parfum", bitolaPerf,   catPerfume, now.AddMonths(14)),
            MakeProduct("Black Opium EDP",              "FRAG-YSL-BLOP-90",   "90ml",  "Eau de Parfum", skopjePerf,   catPerfume, now.AddMonths(10)),
            MakeProduct("Libre EDP",                    "FRAG-YSL-LIBR-50",   "50ml",  "Eau de Parfum", strumicaPerf, catPerfume, now.AddMonths(16)),
            MakeProduct("My Way EDP",                   "FRAG-ARMA-MYWY-90",  "90ml",  "Eau de Parfum", bitolaPerf,   catPerfume, now.AddMonths(15)),
            // ---- new ----
            MakeProduct("L'Interdit EDP",               "FRAG-GIVE-LINT-50",  "50ml",  "Eau de Parfum", velesPerf,    catPerfume, now.AddDays(-30)),  // expired
            MakeProduct("Mon Paris EDP",                "FRAG-YSL-MONP-50",   "50ml",  "Eau de Parfum", skopjePerf,   catPerfume, now.AddDays(-12)),  // expired
            MakeProduct("Bloom EDP",                    "FRAG-GUCC-BLOM-100", "100ml", "Eau de Parfum", bitolaPerf,   catPerfume, now.AddMonths(9)),
            MakeProduct("La Vie Est Belle",             "FRAG-LANC-LVEB-75",  "75ml",  "Eau de Parfum", strumicaPerf, catPerfume, now.AddMonths(11)),
            MakeProduct("Olympéa EDP",                  "FRAG-PACO-OLYM-80",  "80ml",  "Eau de Parfum", ohridPerf,    catPerfume, now.AddMonths(13)),
            MakeProduct("Valentino Donna Born in Roma", "FRAG-VALE-BORM-100", "100ml", "Eau de Parfum", skopjePerf,   catPerfume, now.AddMonths(17)),
            MakeProduct("My Burberry Blush",            "FRAG-BURB-MBB-50",   "50ml",  "Eau de Parfum", velesPerf,    catPerfume, now.AddMonths(7)),
            MakeProduct("Idôle EDP",                    "FRAG-LANC-IDOL-50",  "50ml",  "Eau de Parfum", bitolaPerf,   catPerfume, now.AddMonths(19)),
            MakeProduct("Irresistible EDP",             "FRAG-GIVE-IRRE-35",  "35ml",  "Eau de Parfum", skopjePerf,   catPerfume, now.AddMonths(22)),
            MakeProduct("1 Million Parfum",             "FRAG-PACO-1MLP-100", "100ml", "Eau de Parfum", ohridPerf,    catPerfume, now.AddMonths(20)),
            MakeProduct("My Way Parfum",                "FRAG-ARMA-MYWP-50",  "50ml",  "Eau de Parfum", strumicaPerf, catPerfume, now.AddMonths(24)),
            MakeProduct("Guilty Absolute EDP",          "FRAG-GUCC-GABS-90",  "90ml",  "Eau de Parfum", velesPerf,    catPerfume, now.AddMonths(18)),
            MakeProduct("L'Heure Bleue EDP",            "FRAG-GUER-HBLE-75",  "75ml",  "Eau de Parfum", ohridPerf,    catPerfume, now.AddMonths(26)),
        };

        // ── Eau de Toilette — 15 products ────────────────────────────────
        var edts = new[]
        {
            // ---- existing ----
            MakeProduct("Miss Dior Blooming Bouquet",   "FRAG-DIOR-MISS-50",   "50ml",  "Eau de Toilette", skopjePerf,   catEdt, now.AddDays(18)),
            MakeProduct("Twilly d'Hermès",              "FRAG-HERM-TWIL-85",  "85ml",  "Eau de Toilette", strumicaPerf, catEdt, now.AddMonths(8)),
            MakeProduct("Acqua di Giò EDT",             "FRAG-ARMA-ACQUA-100","100ml", "Eau de Toilette", ohridPerf,    catEdt, now.AddMonths(12)),
            MakeProduct("Versace Eros EDT",             "FRAG-VERS-EROS-100", "100ml", "Eau de Toilette", skopjePerf,   catEdt, now.AddMonths(18)),
            MakeProduct("Bright Crystal EDT",           "FRAG-VERS-BRCR-90",  "90ml",  "Eau de Toilette", ohridPerf,    catEdt, now.AddMonths(20)),
            // ---- new ----
            MakeProduct("BOSS Bottled EDT",             "FRAG-BOSS-BOTT-100", "100ml", "Eau de Toilette", skopjePerf,   catEdt, now.AddMonths(16)),
            MakeProduct("Eternity for Men EDT",         "FRAG-CK-ETER-100",   "100ml", "Eau de Toilette", bitolaPerf,   catEdt, now.AddMonths(14)),
            MakeProduct("The One EDT",                  "FRAG-DG-TONE-100",   "100ml", "Eau de Toilette", strumicaPerf, catEdt, now.AddMonths(10)),
            MakeProduct("Luna Rossa EDT",               "FRAG-PRAD-LUNA-100", "100ml", "Eau de Toilette", ohridPerf,    catEdt, now.AddMonths(22)),
            MakeProduct("Cool Water EDT",               "FRAG-DAVI-CWAT-125", "125ml", "Eau de Toilette", velesPerf,    catEdt, now.AddMonths(6)),
            MakeProduct("Acqua di Giò Profumo",         "FRAG-ARMA-AGPR-75",  "75ml",  "Eau de Toilette", bitolaPerf,   catEdt, now.AddMonths(24)),
            MakeProduct("CK One EDT",                   "FRAG-CK-CONE-200",   "200ml", "Eau de Toilette", strumicaPerf, catEdt, now.AddMonths(13)),
            MakeProduct("Light Blue Pour Femme",        "FRAG-DG-LBLU-100",   "100ml", "Eau de Toilette", skopjePerf,   catEdt, now.AddMonths(17)),
            MakeProduct("Dylan Blue EDT",               "FRAG-VERS-DBLUE-100","100ml", "Eau de Toilette", velesPerf,    catEdt, now.AddMonths(15)),
            MakeProduct("Paradoxe EDT",                 "FRAG-PRAD-PARA-90",  "90ml",  "Eau de Toilette", ohridPerf,    catEdt, now.AddMonths(21)),
        };

        // ── Handbags — 15 products (no expiry) ───────────────────────────
        var handbags = new[]
        {
            // ---- existing ----
            MakeProduct("Classic Flap Medium",          "BAG-CHAN-CFMED",      "Medium", "Handbag", skopjeBags,   catHandbag),
            MakeProduct("Lady Dior Medium",             "BAG-DIOR-LDMED",     "Medium", "Handbag", skopjeBags,   catHandbag),
            MakeProduct("Birkin 30",                    "BAG-HERM-BIR30",     "30cm",   "Handbag", bitolaBags,   catHandbag),
            MakeProduct("Neverfull MM",                 "BAG-LV-NFMM",        "MM",     "Handbag", ohridBags,    catHandbag),
            MakeProduct("GG Marmont Small",             "BAG-GUCC-GGMSM",     "Small",  "Handbag", strumicaBags, catHandbag),
            MakeProduct("Re-Edition 2005",              "BAG-PRAD-RE05",      "Small",  "Handbag", bitolaBags,   catHandbag),
            // ---- new ----
            MakeProduct("Le Cagole Medium",             "BAG-BALE-LCAG-MED",  "Medium", "Handbag", skopjeBags,   catHandbag),
            MakeProduct("City Bag Small",               "BAG-BALE-CITY-SML",  "Small",  "Handbag", bitolaBags,   catHandbag),
            MakeProduct("Cassette Bag",                 "BAG-BV-CASS-MED",    "Medium", "Handbag", strumicaBags, catHandbag),
            MakeProduct("Baguette Small",               "BAG-FEND-BAGU-SML",  "Small",  "Handbag", ohridBags,    catHandbag),
            MakeProduct("Peekaboo Medium",              "BAG-FEND-PEEK-MED",  "Medium", "Handbag", velesBags,    catHandbag),
            MakeProduct("Triomphe Small",               "BAG-CELI-TRIO-SML",  "Small",  "Handbag", skopjeBags,   catHandbag),
            MakeProduct("Saddle Bag Small",             "BAG-DIOR-SADD-SML",  "Small",  "Handbag", bitolaBags,   catHandbag),
            MakeProduct("Diana Mini",                   "BAG-GUCC-DIAN-MINI", "Mini",   "Handbag", velesBags,    catHandbag),
            MakeProduct("Speedy 30",                    "BAG-LV-SPD30",       "30cm",   "Handbag", strumicaBags, catHandbag),
        };

        // ── Shoes — 15 products (no expiry) ──────────────────────────────
        var shoes = new[]
        {
            // ---- existing ----
            MakeProduct("So Kate 120 Pumps",            "SHOE-LOU-SKTE-38",   "EU 38", "Pumps",    skopjeBags,   catShoes),
            MakeProduct("Azia 95 Sandals",              "SHOE-JC-AZIA-37",    "EU 37", "Sandals",  ohridBags,    catShoes),
            MakeProduct("Princetown Loafers",           "SHOE-GUCC-PRIN-39",  "EU 39", "Loafers",  skopjeBags,   catShoes),
            MakeProduct("Monolith Sneakers",            "SHOE-PRAD-MONO-38",  "EU 38", "Sneakers", strumicaBags, catShoes),
            MakeProduct("Tribute Platform Heels",       "SHOE-YSL-TRIB-38",   "EU 38", "Heels",    bitolaBags,   catShoes),
            // ---- new ----
            MakeProduct("Hangisi 105 Pumps",            "SHOE-MB-HANG-37",    "EU 37", "Pumps",    bitolaBags,   catShoes),
            MakeProduct("Rockstud Pumps",               "SHOE-VALE-RKST-38",  "EU 38", "Pumps",    ohridBags,    catShoes),
            MakeProduct("Medusa 95 Pumps",              "SHOE-VERS-MEDU-39",  "EU 39", "Pumps",    skopjeBags,   catShoes),
            MakeProduct("Horsebit Loafer",              "SHOE-GUCC-HBIT-37",  "EU 37", "Loafers",  strumicaBags, catShoes),
            MakeProduct("Décolleté 554 Flats",          "SHOE-MB-DECO-38",    "EU 38", "Flats",    velesBags,    catShoes),
            MakeProduct("Bing 100 Sandals",             "SHOE-JC-BING-38",    "EU 38", "Sandals",  bitolaBags,   catShoes),
            MakeProduct("Lou Lou 55 Pumps",             "SHOE-YSL-LLOU-39",   "EU 39", "Pumps",    skopjeBags,   catShoes),
            MakeProduct("Slingback Kitten Heels",       "SHOE-CHAN-SLTK-37",  "EU 37", "Heels",    ohridBags,    catShoes),
            MakeProduct("Strappy Crystal Sandals",      "SHOE-VALE-STRP-38",  "EU 38", "Sandals",  bitolaBags,   catShoes),
            MakeProduct("Versace La Medusa Mules",      "SHOE-VERS-LMMU-39",  "EU 39", "Mules",    velesBags,    catShoes),
        };

        // ── Makeup — 15 products ──────────────────────────────────────────
        var makeup = new[]
        {
            // ---- existing ----
            MakeProduct("Rouge Dior 999 Lipstick",      "MKP-DIOR-RD999-35",  "3.5g",  "Lipstick",       skopjeSkincare,   catMakeup, now.AddMonths(18)),
            MakeProduct("Les Beiges Foundation",        "MKP-CHAN-LBFND-30",  "30ml",  "Foundation",     bitolaSkincare,   catMakeup, now.AddMonths(20)),
            MakeProduct("Touche Éclat Concealer",       "MKP-YSL-TECL-25",   "2.5ml", "Concealer",      skopjeSkincare,   catMakeup, now.AddMonths(24)),
            MakeProduct("Pillow Talk Lip Liner",        "MKP-CT-PTLL-12",    "1.2g",  "Lip Liner",      strumicaSkincare, catMakeup, now.AddMonths(22)),
            MakeProduct("Airbrush Flawless Finish",     "MKP-CT-AFFW-8",     "8g",    "Setting Powder", ohridSkincare,    catMakeup, now.AddMonths(16)),
            MakeProduct("Skin Tint SPF 30",             "MKP-DIOR-SKNT-30",  "30ml",  "Foundation",     bitolaSkincare,   catMakeup, now.AddMonths(12)),
            // ---- new ----
            MakeProduct("Light Reflecting Powder",      "MKP-NARS-LRPW-10",  "10g",   "Setting Powder", skopjeSkincare,   catMakeup, now.AddMonths(26)),
            MakeProduct("Radiant Creamy Concealer",     "MKP-NARS-RCCN-6",   "6ml",   "Concealer",      bitolaSkincare,   catMakeup, now.AddMonths(28)),
            MakeProduct("Ruby Woo Lipstick",            "MKP-MAC-RBWOO-3",   "3g",    "Lipstick",       strumicaSkincare, catMakeup, now.AddMonths(24)),
            MakeProduct("Studio Fix Powder Plus",       "MKP-MAC-SFXPW-15",  "15g",   "Setting Powder", ohridSkincare,    catMakeup, now.AddMonths(20)),
            MakeProduct("Luminous Silk Foundation",     "MKP-ARMA-LSKF-30",  "30ml",  "Foundation",     velesSkincare,    catMakeup, now.AddMonths(18)),
            MakeProduct("Les Beiges Blush",             "MKP-CHAN-LBBL-8",   "8g",    "Blush",          skopjeSkincare,   catMakeup, now.AddMonths(30)),
            MakeProduct("Hypnôse Drama Mascara",        "MKP-LANC-HPDM-6",   "6ml",   "Mascara",        bitolaSkincare,   catMakeup, now.AddMonths(14)),
            MakeProduct("Idôle Mascara",                "MKP-LANC-IDMA-7",   "7ml",   "Mascara",        strumicaSkincare, catMakeup, now.AddMonths(16)),
            MakeProduct("Rosy Glow Blush",              "MKP-DIOR-RGLW-44",  "4.4g",  "Blush",          ohridSkincare,    catMakeup, now.AddMonths(22)),
        };

        // ── Skincare — 10 products (expiry 12–36 months from now) ────────
        var skincare = new[]
        {
            // ---- existing ----
            MakeProduct("Hydra Beauty Gel Crème",       "SKC-CHAN-HBGC-50",   "50ml",  "Moisturiser",   ohridSkincare,    catSkincare, now.AddMonths(14)),
            // ---- new ----
            MakeProduct("Crème de la Mer",              "SKC-LM-CREME-60",    "60ml",  "Moisturiser",   skopjeSkincare,   catSkincare, now.AddMonths(24)),
            MakeProduct("The Concentrate",              "SKC-LM-CONC-30",     "30ml",  "Serum",         bitolaSkincare,   catSkincare, now.AddMonths(18)),
            MakeProduct("Capture Totale Super Serum",   "SKC-DIOR-CTSS-30",   "30ml",  "Serum",         strumicaSkincare, catSkincare, now.AddMonths(20)),
            MakeProduct("Advanced Night Repair Serum",  "SKC-EL-ANR-50",      "50ml",  "Serum",         ohridSkincare,    catSkincare, now.AddMonths(22)),
            MakeProduct("Facial Treatment Essence",     "SKC-SKII-FTE-230",   "230ml", "Toner",         velesSkincare,    catSkincare, now.AddMonths(30)),
            MakeProduct("Black Rose Skin Infusion Cream","SKC-SIS-BRCM-50",   "50ml",  "Moisturiser",   skopjeSkincare,   catSkincare, now.AddMonths(36)),
            MakeProduct("All About Eyes Cream",         "SKC-EL-AAE-15",      "15ml",  "Eye Cream",     bitolaSkincare,   catSkincare, now.AddMonths(16)),
            MakeProduct("Hydra Beauty Micro Serum",     "SKC-CHAN-HBMS-30",   "30ml",  "Serum",         strumicaSkincare, catSkincare, now.AddMonths(12)),
            MakeProduct("Prestige La Crème Texture",    "SKC-DIOR-PRLC-50",   "50ml",  "Moisturiser",   velesSkincare,    catSkincare, now.AddMonths(28)),
        };

        // ── Accessories — 10 products (no expiry) ────────────────────────
        var accessories = new[]
        {
            MakeProduct("Twilly d'Hermès Silk Scarf",   "ACC-HERM-TWSC-70",   "70×70cm", "Silk Scarf",  skopjeAcc,    catAccessory),
            MakeProduct("Hermès Carré Silk Scarf",      "ACC-HERM-CARR-90",   "90×90cm", "Silk Scarf",  bitolaAcc,    catAccessory),
            MakeProduct("GG Logo Canvas Belt",          "ACC-GUCC-GGBL-85",   "85cm",    "Belt",        strumicaAcc,  catAccessory),
            MakeProduct("Marmont Leather Belt",         "ACC-GUCC-MRBL-90",   "90cm",    "Belt",        ohridAcc,     catAccessory),
            MakeProduct("CC Camellia Brooch",           "ACC-CHAN-CCBR-OS",   "One Size","Brooch",      velesAcc,     catAccessory),
            MakeProduct("Chanel Pearl CC Brooch",       "ACC-CHAN-PRLB-OS",   "One Size","Brooch",      skopjeAcc,    catAccessory),
            MakeProduct("DiorSoLight Sunglasses",       "ACC-DIOR-DSLS-OS",   "One Size","Sunglasses",  bitolaAcc,    catAccessory),
            MakeProduct("Technolor Sunglasses",         "ACC-DIOR-TECH-OS",   "One Size","Sunglasses",  strumicaAcc,  catAccessory),
            MakeProduct("Saffiano Bifold Wallet",       "ACC-PRAD-SAFW-OS",   "One Size","Wallet",      ohridAcc,     catAccessory),
            MakeProduct("Cahier Leather Wallet",        "ACC-PRAD-CAHW-OS",   "One Size","Wallet",      velesAcc,     catAccessory),
        };

        db.Products.AddRange(perfumes);
        db.Products.AddRange(edts);
        db.Products.AddRange(handbags);
        db.Products.AddRange(shoes);
        db.Products.AddRange(makeup);
        db.Products.AddRange(skincare);
        db.Products.AddRange(accessories);

        await db.SaveChangesAsync(ct);
        await searchIndex.RebuildAsync(ct);
    }
}

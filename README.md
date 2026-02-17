<p align="center">
  <h1 align="center">📊 CSV → Code Generator</h1>
  <p align="center">
    <strong>CSV dosyalarınızı anında kullanılabilir koda dönüştürün!</strong>
  </p>
  <p align="center">
    C# • C++ • C • Python • Java
  </p>
</p>

---

## 🎯 Bu Uygulama Ne İşe Yarar?

**CSV → Code Generator**, elinizdeki CSV (virgülle/noktalı virgülle ayrılmış) veri dosyalarını **otomatik olarak programlama koduna** dönüştüren bir masaüstü uygulamadır.

Diyelim ki elinizde bir Excel tablosu veya CSV dosyası var — içinde ürün listesi, şehir bilgileri, oyun verileri, konfigürasyon parametreleri vb. bulunuyor. Bu verileri programınızda kullanmak istiyorsunuz ama her birini tek tek yazmak çok zaman alıyor. İşte bu uygulama, **tüm bu verileri otomatik olarak seçtiğiniz programlama dilinde kullanıma hazır koda** çevirir.

> **Kısacası**: Tablo verisi girer → Programlama kodu çıkar! 🚀

---

## ✨ Özellikler

### 🌍 5 Farklı Programlama Dili Desteği

Verilerinizi aşağıdaki dillerden istediğiniz birine dönüştürebilirsiniz:

| Dil        | Dosya Uzantısı | Açıklama                               |
| ---------- | -------------- | -------------------------------------- |
| **C#**     | `.cs`          | Microsoft'un modern programlama dili   |
| **C++**    | `.h`           | Oyun ve sistem programlama dili        |
| **C**      | `.h`           | Düşük seviyeli sistem programlama dili |
| **Python** | `.py`          | Veri bilimi ve yapay zeka dili         |
| **Java**   | `.java`        | Kurumsal ve Android programlama dili   |

### 📋 Kolon Yönetimi

CSV dosyanızdaki her sütun için şunları ayarlayabilirsiniz:

- **Property Adı**: Kodda kullanılacak ismi değiştirebilirsiniz
- **Tip Seçimi**: Her sütun için uygun veri tipini seçebilirsiniz (metin, sayı, ondalıklı sayı, evet/hayır vb.)
- **Örnek Değer**: Her sütunun ilk satırdaki değerini görebilirsiniz

### 🏷️ Enum (Sabit Değer Listesi) Desteği

Eğer bir sütundaki değerler sınırlı sayıda tekrar ediyorsa (örneğin: "Kırmızı", "Mavi", "Yeşil"), o sütunu **enum** tipine çevirerek daha güvenli ve okunabilir kod üretebilirsiniz.

### 📦 Kolon Gruplama

Birbirine bağlı sütunları (örneğin X, Y, Z koordinatları) tek bir **dizi (array)** veya **liste** altında toplayabilirsiniz. Böylece üretilen kodda bu değerler gruplanmış şekilde yer alır.

### 🔀 Satır Gruplama (Group By)

Verilerinizi belirli bir sütuna göre **gruplandırabilirsiniz**. Örneğin, kategoriye göre gruplama yaparsanız, üretilen kodda her kategori ayrı bir liste olarak oluşturulur.

### 🔎 Lookup (Sözlük/Arama) Yapısı

Bir sütunu **Lookup Key** olarak seçerek, verilerinizi **sözlük (dictionary)** yapısına dönüştürebilirsiniz. Bu sayede belirli bir anahtar değere (örneğin bir ID veya kategori) göre hızlıca veri arayabilirsiniz.

### ⭐ Unique (Benzersiz Değerler) Dizisi

Herhangi bir sütundaki **tekrar etmeyen (benzersiz) değerleri** ayrı bir dizi/liste olarak üretebilirsiniz. Örneğin, bir "Şehir" sütunundaki tüm farklı şehirlerin listesini otomatik olarak çıkarır.

### 📎 Mevcut Dosyaya Ekleme (Append)

Yeni CSV verileri, **daha önce oluşturulmuş bir dosyanın sonuna** eklenebilir. Bu sayede dosyayı baştan oluşturmanıza gerek kalmaz.

### 🧠 Akıllı Veri İşleme

- **Boş/null değerler**: "NA", "NaN", "NULL", "-" ve boş hücreler otomatik olarak algılanır
- **Ondalık ayırıcı**: Hem virgül (123,45) hem nokta (123.45) desteklenir
- **Özel karakterler**: Sütun adlarındaki boşluk, özel karakter vb. otomatik temizlenir

---

## 🖥️ Uygulama Arayüzü

Uygulama 3 ana bölümden oluşur:

```
┌─────────────┬─────────────────────────┬────────────────────┐
│  SOL PANEL  │      ORTA PANEL         │    SAĞ PANEL       │
│             │                         │                    │
│  • Dosya    │  • Kolon Ayarları       │  • Kod Önizleme    │
│    Seçimi   │    (tablo halinde)      │    (renkli kod)    │
│             │                         │                    │
│  • Dışa     │  • Veri Önizleme        │  • Butonlar:       │
│    Aktarma  │    (ham veri tablosu)   │    - Önizle        │
│    Ayarları │                         │    - Kaydet        │
│             │                         │    - Kopyala       │
│  • Hedef    │                         │    - Dosyaya Ekle  │
│    Dil      │                         │                    │
└─────────────┴─────────────────────────┴────────────────────┘
```

### Sol Panel — Ayarlar

| Alan                  | Açıklama                                                 |
| --------------------- | -------------------------------------------------------- |
| **CSV Dosya Seç**     | Bilgisayarınızdan bir CSV dosyası seçmenizi sağlar       |
| **Klasör Seç**        | Bir klasördeki tüm CSV dosyalarını toplu yükler          |
| **Hedef Dil**         | Kodun hangi programlama dilinde üretileceğini belirler   |
| **Sınıf Adı**         | Üretilecek kodun sınıf/yapı adını belirler               |
| **Namespace**         | Kodun paket/isim alanını belirler                        |
| **Group By**          | Satırları hangi sütuna göre gruplamak istediğinizi seçer |
| **Lookup Key**        | Sözlük yapısı için anahtar sütunu seçer                  |
| **Dışa Aktarma Yolu** | Dosyanın nereye kaydedileceğini belirler                 |

### Orta Panel — Kolon Ayarları

Her sütun için düzenleyebileceğiniz bilgiler:

| Sütun            | Açıklama                                             |
| ---------------- | ---------------------------------------------------- |
| **Orijinal Ad**  | CSV dosyasındaki orijinal sütun adı (değiştirilemez) |
| **Property Adı** | Kodda kullanılacak ad (isteğe göre değiştirilebilir) |
| **Tip**          | Veri tipi (string, int, float, bool, enum vb.)       |
| **Grup Adı**     | Birden fazla sütunu gruplamak için ortak ad          |
| **Koleksiyon**   | Grup tipi: None, List veya Array                     |
| **Örnek Değer**  | İlk satırdaki değer (sadece bilgi amaçlı)            |
| **Unique**       | ✅ işaretlenirse benzersiz değerler dizisi üretilir  |

---

## 📖 Kullanım Adımları

### Adım 1: CSV Dosyasını Açın

1. Sol paneldeki **"📄 CSV Dosya Seç"** butonuna tıklayın
2. Bilgisayarınızdan bir `.csv` dosyası seçin
3. Dosya otomatik olarak yüklenir ve sütunlar görüntülenir

### Adım 2: Hedef Dili Seçin

Sol paneldeki **"Hedef Dil"** dropdown'undan istediğiniz programlama dilini seçin.

### Adım 3: Kolon Ayarlarını Yapın

Ortadaki tabloda her sütun için:

- **Tip** sütunundan uygun veri tipini seçin
- İsterseniz **Property Adı**nı değiştirin
- Benzersiz değerler istiyorsanız **Unique** kutucuğunu işaretleyin

### Adım 4: (Opsiyonel) Gruplama veya Lookup Seçin

- Satırları gruplamak için → **Group By** dropdown'undan sütun seçin
- Sözlük yapısı için → **Lookup Key** dropdown'undan sütun seçin

### Adım 5: Kodu Oluşturun

- **"Önizle"** butonu → Kodu sağ panelde gösterir
- **"Kaydet"** butonu → Kodu dosyaya kaydeder
- **"Kopyala"** butonu → Kodu panoya kopyalar

---

## 📝 Örnekler

### Örnek CSV Dosyası

Aşağıdaki CSV dosyasını düşünelim (`urunler.csv`):

```csv
Id;Kategori;Urun;Fiyat;Stok
1;Elektronik;Telefon;5999.90;120
2;Elektronik;Laptop;15999.50;45
3;Giyim;T-Shirt;149.90;500
4;Giyim;Pantolon;399.90;200
5;Gida;Süt;24.90;1000
```

---

### 🟢 Örnek 1 — Temel Kod Üretimi (C#)

Herhangi bir ekstra ayar yapmadan direkt **"Önizle"** butonuna basarsanız:

```csharp
namespace Generated
{
    public class Urunler
    {
        public int Id { get; set; }
        public string Kategori { get; set; }
        public string Urun { get; set; }
        public double Fiyat { get; set; }
        public int Stok { get; set; }

        #region Data

        public static readonly List<Urunler> Items = new()
        {
            new Urunler
            {
                Id = 1,
                Kategori = "Elektronik",
                Urun = "Telefon",
                Fiyat = 5999.90,
                Stok = 120
            },
            new Urunler
            {
                Id = 2,
                Kategori = "Elektronik",
                Urun = "Laptop",
                Fiyat = 15999.50,
                Stok = 45
            },
            // ... diğer satırlar
        };

        #endregion
    }
}
```

---

### 🔵 Örnek 2 — Python ile Kod Üretimi

Hedef dil olarak **Python** seçildiğinde:

```python
from dataclasses import dataclass, field
from enum import Enum
from typing import Optional, List, Dict

@dataclass
class Urunler:
    id: int
    kategori: str
    urun: str
    fiyat: float
    stok: int

Items: List[Urunler] = [
    Urunler(id=1, kategori="Elektronik", urun="Telefon", fiyat=5999.90, stok=120),
    Urunler(id=2, kategori="Elektronik", urun="Laptop", fiyat=15999.50, stok=45),
    Urunler(id=3, kategori="Giyim", urun="T-Shirt", fiyat=149.90, stok=500),
    # ... diğer satırlar
]
```

---

### 🟡 Örnek 3 — Enum Kullanımı

**Kategori** sütununun tipini `enum` olarak ayarlarsanız:

```csharp
namespace Generated
{
    public enum UrunlerKategori
    {
        Elektronik,
        Giyim,
        Gida
    }

    public class Urunler
    {
        public int Id { get; set; }
        public UrunlerKategori Kategori { get; set; }
        public string Urun { get; set; }
        // ...

        public static readonly List<Urunler> Items = new()
        {
            new Urunler
            {
                Id = 1,
                Kategori = UrunlerKategori.Elektronik,
                Urun = "Telefon",
                // ...
            },
            // ...
        };
    }
}
```

---

### 🟠 Örnek 4 — Group By (Gruplama)

**Group By** olarak **Kategori** sütununu seçerseniz, veriler kategoriye göre gruplanır:

```csharp
public static readonly List<List<Urunler>> GroupedItems = new()
{
    // Group: Elektronik
    new List<Urunler>()
    {
        new Urunler { Id = 1, Urun = "Telefon", Fiyat = 5999.90 },
        new Urunler { Id = 2, Urun = "Laptop", Fiyat = 15999.50 }
    },
    // Group: Giyim
    new List<Urunler>()
    {
        new Urunler { Id = 3, Urun = "T-Shirt", Fiyat = 149.90 },
        new Urunler { Id = 4, Urun = "Pantolon", Fiyat = 399.90 }
    },
    // Group: Gida
    new List<Urunler>()
    {
        new Urunler { Id = 5, Urun = "Süt", Fiyat = 24.90 }
    }
};
```

---

### 🔴 Örnek 5 — Lookup Key (Sözlük Yapısı)

**Lookup Key** olarak **Kategori** sütununu seçerseniz, veriler bir **sözlük** (dictionary) yapısına dönüşür. Bu yapıda, herhangi bir kategori adını yazarak o kategoriye ait tüm ürünlere anında erişebilirsiniz:

```csharp
public static readonly Dictionary<string, List<Urunler>> ItemsByKategori = new()
{
    ["Elektronik"] = new List<Urunler>()
    {
        new Urunler { Id = 1, Urun = "Telefon", Fiyat = 5999.90 },
        new Urunler { Id = 2, Urun = "Laptop", Fiyat = 15999.50 }
    },
    ["Giyim"] = new List<Urunler>()
    {
        new Urunler { Id = 3, Urun = "T-Shirt", Fiyat = 149.90 },
        new Urunler { Id = 4, Urun = "Pantolon", Fiyat = 399.90 }
    },
    ["Gida"] = new List<Urunler>()
    {
        new Urunler { Id = 5, Urun = "Süt", Fiyat = 24.90 }
    }
};
```

> **Kullanımı**: `ItemsByKategori["Elektronik"]` yazarak elektronik ürünlerin listesine doğrudan ulaşabilirsiniz.

---

### 🟣 Örnek 6 — Unique (Benzersiz Değerler)

**Kategori** sütunu için **Unique** kutucuğunu ✅ işaretlerseniz, tekrar etmeyen tüm kategori isimleri ayrı bir dizi olarak üretilir:

```csharp
#region Unique Values

public static readonly string[] UniqueKategori = { "Elektronik", "Giyim", "Gida" };

#endregion
```

> **Ne İşe Yarar?** Bir dropdown/combobox doldurmak, filtreleme seçenekleri sunmak veya veri doğrulama yapmak için idealdir.

---

### 🔶 Örnek 7 — Kolon Gruplama

CSV'deki **OutXXX**, **OutYYY**, **OutZZZ** gibi sütunlara aynı **Grup Adı** (örneğin: "Outputs") verirseniz:

```csharp
public class MyData
{
    public int Id { get; set; }
    public List<double> Outputs { get; set; }  // OutXXX, OutYYY, OutZZZ birleşti!

    public static readonly List<MyData> Items = new()
    {
        new MyData
        {
            Id = 1,
            Outputs = new List<double> { 12.6, 231, 232 }
        },
        // ...
    };
}
```

---

### ☕ Örnek 8 — Java ile Üretim

Hedef dil olarak **Java** seçildiğinde:

```java
package generated;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

public class Urunler {

    public int id;
    public String kategori;
    public String urun;
    public double fiyat;
    public int stok;

    public Urunler(int id, String kategori, String urun, double fiyat, int stok) {
        this.id = id;
        this.kategori = kategori;
        this.urun = urun;
        this.fiyat = fiyat;
        this.stok = stok;
    }

    public static final List<Urunler> Items = new ArrayList<>(Arrays.asList(
        new Urunler(1, "Elektronik", "Telefon", 5999.90, 120),
        new Urunler(2, "Elektronik", "Laptop", 15999.50, 45)
        // ...
    ));
}
```

---

### 🔧 Örnek 9 — C++ ile Üretim

Hedef dil olarak **C++** seçildiğinde header-only dosya üretilir:

```cpp
#pragma once
#include <string>
#include <vector>
#include <array>

namespace Generated
{
    struct Urunler
    {
        int Id;
        std::string Kategori;
        std::string Urun;
        double Fiyat;
        int Stok;
    };

    inline const std::vector<Urunler> Items = {
        Urunler{
            /* .Id = */ 1,
            /* .Kategori = */ "Elektronik",
            /* .Urun = */ "Telefon",
            /* .Fiyat = */ 5999.90,
            /* .Stok = */ 120
        },
        // ...
    };
}
```

---

## 🔄 Özellik Karşılaştırma Tablosu

| Özellik                   | C#  | C++ |  C   | Python | Java |
| ------------------------- | :-: | :-: | :--: | :----: | :--: |
| Temel veri üretimi        | ✅  | ✅  |  ✅  |   ✅   |  ✅  |
| Enum desteği              | ✅  | ✅  |  ✅  |   ✅   |  ✅  |
| Kolon gruplama            | ✅  | ✅  |  ✅  |   ✅   |  ✅  |
| Satır gruplama (Group By) | ✅  | ✅  |  ✅  |   ✅   |  ✅  |
| Lookup (Sözlük)           | ✅  | ✅  | ✅\* |   ✅   |  ✅  |
| Unique diziler            | ✅  | ✅  |  ✅  |   ✅   |  ✅  |
| Dosyaya ekleme (Append)   | ✅  | ✅  |  ✅  |   ✅   |  ✅  |
| Null/NA/NaN işleme        | ✅  | ✅  |  ✅  |   ✅   |  ✅  |

> _\* C dilinde dictionary yapısı olmadığından, her anahtar için ayrı statik array üretilir._

---

## ⚙️ Desteklenen Veri Tipleri

Her dil için kullanılabilir veri tipleri:

| C#        | C++             | C               | Python  | Java      |
| --------- | --------------- | --------------- | ------- | --------- |
| `string`  | `std::string`   | `const char*`   | `str`   | `String`  |
| `int`     | `int`           | `int`           | `int`   | `int`     |
| `long`    | `long`          | `long`          | `int`   | `long`    |
| `float`   | `float`         | `float`         | `float` | `float`   |
| `double`  | `double`        | `double`        | `float` | `double`  |
| `decimal` | `double`        | `double`        | `float` | `double`  |
| `bool`    | `bool`          | —               | `bool`  | `boolean` |
| `char`    | `char`          | `char`          | `str`   | `char`    |
| `byte`    | `unsigned char` | `unsigned char` | `int`   | `byte`    |
| `enum`    | `enum class`    | `typedef enum`  | `Enum`  | `enum`    |

---

## 💡 İpuçları

1. **Ondalık ayırıcı**: Hem virgül (`,`) hem nokta (`.`) kullanabilirsiniz. Uygulama ikisini de doğru yorumlar.

2. **Boş değerler**: CSV'deki boş hücreler, `NA`, `NaN`, `NULL` veya `-` değerleri otomatik olarak uygun varsayılan değerlere dönüştürülür.

3. **Group By vs Lookup Key**: İkisi aynı anda kullanılamaz. Group By seçiliyse Lookup Key devre dışı kalır.

4. **Unique + Lookup birlikte**: Unique checkbox'ları Lookup Key veya Group By ile birlikte kullanılabilir. Unique diziler her zaman ayrı olarak üretilir.

5. **Toplu dosya işleme**: Sol panelden bir klasör seçerek birden fazla CSV dosyasını aynı anda yükleyebilir ve her birini ayrı ayrı işleyebilirsiniz.

---

## 🛠️ Teknik Bilgiler

- **Platform**: Windows (WinForms)
- **Framework**: .NET 8
- **Mimari**: Strategy Pattern + Facade Pattern
- **Dil**: C#

---

## 📄 Lisans

Bu proje özel kullanım amaçlıdır.

---

<p align="center">
  <strong>CSV → Code Generator</strong> ile verilerinizi anında koda dönüştürün! 🎉
</p>

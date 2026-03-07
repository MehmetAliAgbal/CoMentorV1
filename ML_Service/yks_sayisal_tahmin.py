# -*- coding: utf-8 -*-
"""
YKS Sayısal Net Tahmin Sistemi
Makine öğrenimi ve trend analizi ile sınav net tahmini
"""

import pandas as pd
import numpy as np
from sklearn.ensemble import GradientBoostingRegressor
from sklearn.linear_model import LinearRegression
from sklearn.preprocessing import StandardScaler
from datetime import datetime, timedelta
import warnings
import os

warnings.filterwarnings('ignore')

# ═══════════════════════════════════════════════════════════════════════════════
# DENEME TARİHLERİ VE DÖNEM BİLGİLERİ
# ═══════════════════════════════════════════════════════════════════════════════

# YKS Sayısal denemeleri için dönem bilgileri
DONEMLER = [
    {'id': 1, 'tarih': datetime(2024, 9, 15), 'donem_adi': 'Deneme 1 (Eylül)'},
    {'id': 6, 'tarih': datetime(2024, 11, 15), 'donem_adi': 'Deneme 6 (Kasım)'},
    {'id': 13, 'tarih': datetime(2025, 2, 15), 'donem_adi': 'Deneme 13 (Şubat)'},
    {'id': 15, 'tarih': datetime(2025, 3, 15), 'donem_adi': 'Deneme 15 (Mart)'},
    {'id': 17, 'tarih': datetime(2025, 4, 15), 'donem_adi': 'Deneme 17 (Nisan)'},
]

# YKS 2025 tahmini tarih
SINAV_TARIHI = datetime(2025, 6, 21)

# ═══════════════════════════════════════════════════════════════════════════════
# RENK KODLARI
# ═══════════════════════════════════════════════════════════════════════════════

class Renk:
    MOR = '\033[95m'
    MAVI = '\033[94m'
    CYAN = '\033[96m'
    YESIL = '\033[92m'
    SARI = '\033[93m'
    KIRMIZI = '\033[91m'
    BOLD = '\033[1m'
    DIM = '\033[2m'
    RESET = '\033[0m'

# ═══════════════════════════════════════════════════════════════════════════════
# YARDIMCI FONKSİYONLAR
# ═══════════════════════════════════════════════════════════════════════════════

def temizle():
    os.system('cls' if os.name == 'nt' else 'clear')

def banner():
    print(f"""
{Renk.BOLD}{Renk.CYAN}YKS SAYISAL TAHMİN SİSTEMİ{Renk.RESET}
{Renk.DIM}Trend Analizi + ML ile Sınav Başarı Tahmini{Renk.RESET}
""")

def kutu(metin, renk=Renk.CYAN, genislik=60):
    print(f"\n{renk}{Renk.BOLD}[ {metin} ]{Renk.RESET}")

def bilgi(metin, simge="ℹ️"):
    print(f"  {simge} {Renk.CYAN}{metin}{Renk.RESET}")

def basari(metin):
    print(f"  {Renk.YESIL}✓ {metin}{Renk.RESET}")

def uyari(metin):
    print(f"  {Renk.SARI}⚠ {metin}{Renk.RESET}")

def hata(metin):
    print(f"  {Renk.KIRMIZI}✗ {metin}{Renk.RESET}")

# ═══════════════════════════════════════════════════════════════════════════════
# VERİ YÖNETİMİ
# ═══════════════════════════════════════════════════════════════════════════════

def veri_yukle():
    """Excel/CSV dosyasından verileri yükler"""
    try:
        script_dir = os.path.dirname(os.path.abspath(__file__))
        
        csv_path = os.path.join(script_dir, 'denemler_ml_say.csv')
        excel_path = os.path.join(script_dir, 'denemler_ml_say.xlsx')
        
        if os.path.exists(csv_path):
            df = pd.read_csv(csv_path)
        else:
            df = pd.read_excel(excel_path)
        
        # Eğer veri tek bir sütundaysa ve virgülle ayrılmışsa (CSV gibi kaydedilmişse)
        if len(df.columns) == 1 and ',' in str(df.columns[0]):
            col_name = df.columns[0]
            headers = col_name.split(',')
            data = df[col_name].astype(str).str.split(',', expand=True)
            data.columns = headers
            df = data
            
        df = df.dropna(subset=['ogr_no'])
        df = df[df['ogr_no'] != 'nan']
        
        # Sayısal veri tipine dönüştür
        for col in df.columns:
            if col != 'ogr_no':
                df[col] = pd.to_numeric(df[col], errors='coerce')
        
        # Toplam net sütunlarını hesapla
        donemler_id = [1, 6, 13, 15, 17]
        for donem in donemler_id:
            mat_col = f'matematik_say{donem}'
            fen_col = f'fen_say{donem}'
            toplam_col = f'toplam_{donem}'
            if mat_col in df.columns and fen_col in df.columns:
                df[toplam_col] = df[mat_col] + df[fen_col]
        
        df = df.replace([np.inf, -np.inf], np.nan)
        # Sadece TÜM deneme verileri boş olan öğrencileri sil
        feature_cols = [col for col in df.columns if col != 'ogr_no']
        df = df.dropna(how='all', subset=feature_cols)
        
        return df
    except Exception as e:
        hata(f"Veri yükleme hatası: {e}")
        return None

# ═══════════════════════════════════════════════════════════════════════════════
# TREND ANALİZİ
# ═══════════════════════════════════════════════════════════════════════════════

def gun_farki(tarih1, tarih2):
    """İki tarih arasındaki gün farkı"""
    return (tarih2 - tarih1).days

def donem_tarihi_al(donem_id):
    """Dönem ID'sine göre tarih döndürür"""
    for donem in DONEMLER:
        if donem['id'] == donem_id:
            return donem['tarih']
    return datetime.now()

def trend_hesapla(denemeler):
    """
    Girilen denemelere göre trend analizi yapar
    """
    if len(denemeler) < 2:
        return None
    
    # Tarihe göre sırala
    sirali = sorted(denemeler.items(), key=lambda x: donem_tarihi_al(x[0]))
    
    dersler = ['matematik', 'fen']
    trendler = {}
    
    for ders in dersler:
        gunler = []
        netler = []
        
        baslangic_tarihi = donem_tarihi_al(sirali[0][0])
        
        for donem_id, netler_dict in sirali:
            tarih = donem_tarihi_al(donem_id)
            gun = gun_farki(baslangic_tarihi, tarih)
            gunler.append(gun)
            netler.append(netler_dict[ders])
        
        X = np.array(gunler).reshape(-1, 1)
        y = np.array(netler)
        
        model = LinearRegression()
        model.fit(X, y)
        
        trendler[ders] = {
            'slope': model.coef_[0],
            'intercept': model.intercept_,
            'baslangic': netler[0],
            'son': netler[-1],
            'model': model
        }
    
    # Toplam net trendi
    toplam_gunler = []
    toplam_netler = []
    
    baslangic_tarihi = donem_tarihi_al(sirali[0][0])
    
    for donem_id, netler_dict in sirali:
        tarih = donem_tarihi_al(donem_id)
        gun = gun_farki(baslangic_tarihi, tarih)
        toplam_gunler.append(gun)
        toplam_netler.append(sum(netler_dict.values()))
    
    X = np.array(toplam_gunler).reshape(-1, 1)
    y = np.array(toplam_netler)
    
    model = LinearRegression()
    model.fit(X, y)
    
    trendler['toplam'] = {
        'slope': model.coef_[0],
        'intercept': model.intercept_,
        'baslangic': toplam_netler[0],
        'son': toplam_netler[-1],
        'model': model,
        'gunler': toplam_gunler,
        'netler': toplam_netler
    }
    
    return trendler, sirali

def sinav_tahmini_hesapla(trendler, sirali_denemeler, sinav_tarihi):
    """Sınav gününe kadar trend extrapolasyonu"""
    
    baslangic_tarihi = donem_tarihi_al(sirali_denemeler[0][0])
    sinav_gunu = gun_farki(baslangic_tarihi, sinav_tarihi)
    
    tahminler = {}
    dersler = ['matematik', 'fen']
    max_netler = {'matematik': 40, 'fen': 40}
    
    for ders in dersler:
        trend = trendler[ders]
        tahmin = trend['model'].predict([[sinav_gunu]])[0]
        tahmin = max(0, min(tahmin, max_netler[ders]))
        tahminler[ders] = tahmin
    
    toplam_tahmin = trendler['toplam']['model'].predict([[sinav_gunu]])[0]
    toplam_tahmin = max(0, min(toplam_tahmin, 80))
    
    tahminler['toplam'] = toplam_tahmin
    
    return tahminler

# ═══════════════════════════════════════════════════════════════════════════════
# ML MODELİ
# ═══════════════════════════════════════════════════════════════════════════════

def ml_tahmin(df, kullanici_denemeler):
    """
    Veri setindeki benzer öğrencileri bularak ML tahmini yapar
    """
    if len(kullanici_denemeler) == 0:
        return None
    
    dersler = ['matematik', 'fen']
    deneme_sutunlari = {
        1: ['matematik_say1', 'fen_say1', 'toplam_1'],
        6: ['matematik_say6', 'fen_say6', 'toplam_6'],
        13: ['matematik_say13', 'fen_say13', 'toplam_13'],
        15: ['matematik_say15', 'fen_say15', 'toplam_15'],
        17: ['matematik_say17', 'fen_say17', 'toplam_17'],
    }
    
    feature_cols = []
    user_features = []
    
    for donem_id in sorted(kullanici_denemeler.keys()):
        if donem_id in deneme_sutunlari:
            sutunlar = deneme_sutunlari[donem_id][:2]
            for i, ders in enumerate(dersler):
                col = sutunlar[i]
                if col in df.columns:
                    feature_cols.append(col)
                    user_features.append(kullanici_denemeler[donem_id][ders])
    
    if len(feature_cols) == 0:
        return None
    
    target_deneme = 17
    target_cols = deneme_sutunlari.get(target_deneme, [])
    
    if not all(col in df.columns for col in target_cols):
        target_deneme = 15
        target_cols = deneme_sutunlari.get(target_deneme, [])
    
    if not target_cols or not all(col in df.columns for col in target_cols):
        return None
    
    valid_rows = df[feature_cols + target_cols].dropna()
    
    if len(valid_rows) < 10:
        return None
    
    X = valid_rows[feature_cols].values
    
    tahminler = {}
    
    for i, ders in enumerate(dersler):
        target_col = target_cols[i]
        y = valid_rows[target_col].values
        
        scaler = StandardScaler()
        X_scaled = scaler.fit_transform(X)
        
        model = GradientBoostingRegressor(n_estimators=100, max_depth=4, random_state=42)
        model.fit(X_scaled, y)
        
        user_X = np.array(user_features).reshape(1, -1)
        user_X_scaled = scaler.transform(user_X)
        
        tahmin = model.predict(user_X_scaled)[0]
        max_net = 40
        tahmin = max(0, min(tahmin, max_net))
        tahminler[ders] = tahmin
    
    toplam_col = f'toplam_{target_deneme}'
    y = valid_rows[toplam_col].values
    
    scaler = StandardScaler()
    X_scaled = scaler.fit_transform(X)
    
    model = GradientBoostingRegressor(n_estimators=100, max_depth=4, random_state=42)
    model.fit(X_scaled, y)
    
    user_X = np.array(user_features).reshape(1, -1)
    user_X_scaled = scaler.transform(user_X)
    
    tahminler['toplam'] = max(0, min(model.predict(user_X_scaled)[0], 80))
    
    return tahminler

def benzer_ogrenciler_bul(df, kullanici_denemeler):
    """Veri setindeki benzer öğrencilerin gelişimini analiz eder"""
    
    if len(kullanici_denemeler) == 0:
        return None
    
    son_donem = max(kullanici_denemeler.keys())
    kullanici_toplam = sum(kullanici_denemeler[son_donem].values())
    
    toplam_col = f'toplam_{son_donem}'
    
    if toplam_col not in df.columns:
        return None
    
    benzerler = df[(df[toplam_col] >= kullanici_toplam - 5) & 
                   (df[toplam_col] <= kullanici_toplam + 5)]
    
    if len(benzerler) < 3:
        benzerler = df[(df[toplam_col] >= kullanici_toplam - 10) & 
                       (df[toplam_col] <= kullanici_toplam + 10)]
    
    if len(benzerler) == 0:
        return None
    
    sonuclar = {
        'benzer_sayisi': len(benzerler),
        'ortalama_gelisim': {},
    }
    
    if 'toplam_17' in df.columns and son_donem != 17:
        son_toplam = benzerler['toplam_17'].mean()
        mevcut_toplam = benzerler[toplam_col].mean()
        sonuclar['ortalama_gelisim']['toplam'] = son_toplam - mevcut_toplam
        sonuclar['son_ortalama'] = son_toplam
    
    return sonuclar

# ═══════════════════════════════════════════════════════════════════════════════
# KULLANICI ARAYÜZÜ
# ═══════════════════════════════════════════════════════════════════════════════

def net_al(ders_adi, max_net):
    """Kullanıcıdan net değeri alır"""
    while True:
        try:
            girdi = input(f"      {Renk.BOLD}{ders_adi:12}{Renk.RESET} ({Renk.DIM}0-{max_net}{Renk.RESET}): ")
            if girdi.strip() == '':
                return None
            deger = float(girdi.replace(',', '.'))
            if -10 <= deger <= max_net:
                return deger
            else:
                uyari(f"Lütfen -10 ile {max_net} arasında bir değer girin")
        except ValueError:
            hata("Geçersiz sayı formatı")

def deneme_girisi():
    """Kullanıcıdan deneme netleri alır"""
    kutu("DENEME NETLERİNİZİ GİRİN", Renk.YESIL)
    
    print(f"  {Renk.DIM}Her dönem için netleri girin. Atlamak için Enter'a basın.{Renk.RESET}\n")
    
    denemeler = {}
    
    for i, donem in enumerate(DONEMLER):
        tarih_str = donem['tarih'].strftime("%d.%m.%Y")
        
        print(f"\n  {Renk.BOLD}{donem['donem_adi']}{Renk.RESET} {Renk.DIM}({tarih_str}){Renk.RESET}")
        
        netler = {}
        
        matematik = net_al("Matematik", 40)
        if matematik is not None:
            netler['matematik'] = matematik
            
            fen = net_al("Fen", 40)
            netler['fen'] = fen if fen is not None else 0
            
            denemeler[donem['id']] = netler
            toplam = sum(netler.values())
            print(f"  {Renk.YESIL}Kaydedildi: {toplam:.1f} net{Renk.RESET}")
        else:
            print(f"  {Renk.DIM}Atlandı{Renk.RESET}")
    
    return denemeler

def sinav_tarihi_al():
    """Kullanıcıdan sınav tarihini alır"""
    print(f"\n  {Renk.DIM}Varsayılan: {SINAV_TARIHI.strftime('%d.%m.%Y')}{Renk.RESET}")
    
    while True:
        girdi = input(f"  Sınav tarihi (GG.AA.YYYY): ")
        
        if girdi.strip() == '':
            return SINAV_TARIHI
        
        try:
            tarih = datetime.strptime(girdi.strip(), "%d.%m.%Y")
            return tarih
        except ValueError:
            hata("Geçersiz format")

def sonuclari_goster(denemeler, trend_tahmin, ml_tahmin_sonuc, benzerler, sinav_tarihi):
    """Tüm analiz sonuçlarını gösterir"""
    
    bugun = datetime.now()
    kalan_gun = (sinav_tarihi - bugun).days
    
    kutu(f"ANALİZ SONUÇLARI - Sınava {kalan_gun} Gün", Renk.CYAN)
    
    # GİRİLEN DENEMELER
    print(f"\n  {Renk.BOLD}Girilen Denemeler:{Renk.RESET}")
    print(f"  {'Dönem':<18} {'Mat':>6} {'Fen':>6} {'TOP':>7}")
    print(f"  {'-'*43}")
    
    sirali = sorted(denemeler.items(), key=lambda x: x[0])
    
    for donem_id, netler in sirali:
        toplam = sum(netler.values())
        donem_adi = ""
        for d in DONEMLER:
            if d['id'] == donem_id:
                donem_adi = d['donem_adi']
                break
        print(f"  {donem_adi:<18} {netler['matematik']:>6.1f} {netler['fen']:>6.1f} {Renk.BOLD}{toplam:>7.1f}{Renk.RESET}")
    
    # TREND ANALİZİ
    if len(denemeler) >= 2 and trend_tahmin:
        print(f"\n  {Renk.BOLD}Trend Analizi:{Renk.RESET}")
        
        trendler, _ = trend_hesapla(denemeler)
        gunluk_artis = trendler['toplam']['slope']
        
        trend_renk = Renk.YESIL if gunluk_artis >= 0 else Renk.KIRMIZI
        print(f"  {trend_renk}Günlük: {gunluk_artis:+.3f} net | Haftalık: {gunluk_artis*7:+.2f} net{Renk.RESET}")
        print(f"  {trend_renk}Sınava kadar potansiyel: {gunluk_artis * kalan_gun:+.1f} net{Renk.RESET}")
    
    # TAHMİN SONUÇLARI
    print(f"\n  {Renk.BOLD}Sınav Tahminleri ({sinav_tarihi.strftime('%d.%m.%Y')}):{Renk.RESET}")
    print(f"  {'Yöntem':<18} {'Mat':>6} {'Fen':>6} {'TOP':>7}")
    print(f"  {'-'*43}")
    
    if trend_tahmin:
        t = trend_tahmin
        print(f"  {'Trend':<18} {t['matematik']:>6.1f} {t['fen']:>6.1f} {t['toplam']:>7.1f}")
    
    if ml_tahmin_sonuc:
        m = ml_tahmin_sonuc
        print(f"  {'ML Modeli':<18} {m['matematik']:>6.1f} {m['fen']:>6.1f} {m['toplam']:>7.1f}")
    
    # Ortalama tahmin
    ort = None
    if trend_tahmin and ml_tahmin_sonuc:
        ort = {
            'matematik': (trend_tahmin['matematik'] + ml_tahmin_sonuc['matematik']) / 2,
            'fen': (trend_tahmin['fen'] + ml_tahmin_sonuc['fen']) / 2,
            'toplam': (trend_tahmin['toplam'] + ml_tahmin_sonuc['toplam']) / 2,
        }
    elif trend_tahmin:
        ort = trend_tahmin
    elif ml_tahmin_sonuc:
        ort = ml_tahmin_sonuc
    
    if ort:
        print(f"  {'-'*43}")
        print(f"  {Renk.BOLD}{Renk.YESIL}{'ORTALAMA':<18} {ort['matematik']:>6.1f} {ort['fen']:>6.1f} {ort['toplam']:>7.1f}{Renk.RESET}")
    
    # BENZER ÖĞRENCİ ANALİZİ
    if benzerler:
        print(f"\n  {Renk.BOLD}Benzer Öğrenci Analizi:{Renk.RESET}")
        print(f"  Benzer {benzerler['benzer_sayisi']} öğrenci bulundu")
        
        if 'son_ortalama' in benzerler:
            print(f"  Son deneme ortalaması: {Renk.YESIL}{benzerler['son_ortalama']:.1f} net{Renk.RESET}")
        
        if 'toplam' in benzerler.get('ortalama_gelisim', {}):
            gelisim = benzerler['ortalama_gelisim']['toplam']
            renk = Renk.YESIL if gelisim >= 0 else Renk.KIRMIZI
            print(f"  Ortalama gelişim: {renk}{gelisim:+.1f} net{Renk.RESET}")
    
    # GELİŞİM GRAFİĞİ
    print(f"\n  {Renk.BOLD}Gelişim:{Renk.RESET}")
    
    max_bar = 30
    max_net = 80
    
    for donem_id, netler in sirali:
        toplam = sum(netler.values())
        bar_len = int((toplam / max_net) * max_bar)
        bar = "#" * bar_len
        
        donem_adi = ""
        for d in DONEMLER:
            if d['id'] == donem_id:
                donem_adi = d['donem_adi'][:10]
                break
        
        print(f"  {donem_adi:>12} {bar} {toplam:.1f}")
    
    if ort:
        bar_len = int((ort['toplam'] / max_net) * max_bar)
        bar = "=" * bar_len
        print(f"  {Renk.YESIL}{'Tahmin':>12} {bar} {ort['toplam']:.1f}{Renk.RESET}")

def ana_menu():
    """Ana menü"""
    print(f"""
  {Renk.BOLD}MENÜ{Renk.RESET}
  {Renk.YESIL}[1]{Renk.RESET} Yeni Tahmin Yap
  {Renk.CYAN}[2]{Renk.RESET} Hakkında
  {Renk.DIM}[0]{Renk.RESET} Çıkış
""")
    return input(f"  Seçim: ")

def hakkinda():
    """Hakkında bilgisi"""
    print(f"""
  {Renk.BOLD}HAKKINDA{Renk.RESET}
  
  YKS Sayısal Net Tahmin Sistemi
  Deneme sonuçlarını analiz ederek YKS (Sayısal) net tahmini yapar.

  {Renk.BOLD}Dönemler:{Renk.RESET}
  - Deneme 1: Eylül
  - Deneme 6: Kasım
  - Deneme 13: Şubat
  - Deneme 15: Mart
  - Deneme 17: Nisan

  {Renk.BOLD}Nasıl Çalışır:{Renk.RESET}
  1. Trend Analizi: Gelişim hızınızı hesaplar
  2. ML Modeli: Benzer öğrencilerden öğrenir
  3. Karşılaştırma: Aynı seviyedekileri gösterir

  {Renk.DIM}Ne kadar çok dönem girerseniz tahmin o kadar doğru olur.{Renk.RESET}
""")
    input(f"  Enter'a basın...")

# ═══════════════════════════════════════════════════════════════════════════════
# ANA UYGULAMA
# ═══════════════════════════════════════════════════════════════════════════════

def main():
    """Ana program"""
    
    if os.name == 'nt':
        os.system('color')
    
    temizle()
    banner()
    
    print(f"  Veri yükleniyor...")
    df = veri_yukle()
    
    if df is not None:
        basari(f"{len(df)} öğrenci verisi yüklendi")
    else:
        uyari("Veri yüklenemedi, sadece trend analizi kullanılacak")
        df = pd.DataFrame()
    
    while True:
        secim = ana_menu()
        
        if secim == '0':
            print(f"\n  Görüşürüz!\n")
            break
        
        elif secim == '1':
            denemeler = deneme_girisi()
            
            if len(denemeler) == 0:
                hata("En az 1 dönem girmelisiniz!")
                input(f"\n  Enter'a basın...")
                continue
            
            sinav_tarihi = sinav_tarihi_al()
            
            print(f"\n  Analiz yapılıyor...")
            
            trend_tahmin = None
            if len(denemeler) >= 2:
                trendler, sirali = trend_hesapla(denemeler)
                trend_tahmin = sinav_tahmini_hesapla(trendler, sirali, sinav_tarihi)
            else:
                uyari("Trend için en az 2 dönem gerekli")
            
            ml_sonuc = None
            if len(df) > 0:
                ml_sonuc = ml_tahmin(df, denemeler)
            
            benzerler = None
            if len(df) > 0:
                benzerler = benzer_ogrenciler_bul(df, denemeler)
            
            sonuclari_goster(denemeler, trend_tahmin, ml_sonuc, benzerler, sinav_tarihi)
            
            input(f"\n  Enter'a basın...")
        
        elif secim == '2':
            hakkinda()
        
        else:
            hata("Geçersiz seçim")

if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print(f"\n\n  {Renk.SARI}Program sonlandırıldı.{Renk.RESET}\n")
    except Exception as e:
        print(f"\n  {Renk.KIRMIZI}Hata: {e}{Renk.RESET}\n")

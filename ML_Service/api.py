from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import Dict, Optional
from datetime import datetime
import os
import sys

# Bulunduğu dizini sys.path'e eklerek modülleri sorunsuz yüklemesini sağlıyoruz
current_dir = os.path.dirname(os.path.abspath(__file__))
if current_dir not in sys.path:
    sys.path.append(current_dir)

# Mevcut makine öğrenmesi fonksiyonlarını içeri aktarıyoruz
# Not: yks_sayisal_tahmin.py ve deneme_tahmin.py aynı klasörde olmalı
try:
    import yks_sayisal_tahmin as sayisal
    import deneme_tahmin as tyt
except ImportError as e:
    print(f"Modül içe aktarma hatası: {e}")
    # Modüller bulunamazsa sunucu kilitlenmemesi için
    sayisal = None
    tyt = None

app = FastAPI(
    title="YKS & TYT Net Tahmin API", 
    description="CoMentor projesi için Makine Öğrenmesi destekli Sınav Net Tahmini API'si",
    version="1.0.0"
)

# --- Pydantic Data Modelleri (Gelen İstekleri Doğrulamak İçin) ---

class SayisalDeneme(BaseModel):
    matematik: float
    fen: float

class TYTDeneme(BaseModel):
    turkce: float
    matematik: float
    fen: float
    sosyal: float

class SayisalRequest(BaseModel):
    # Dict[int, ...] -> Period IDs mapping to the trial result. 
    # Example: {"1": {"matematik": 35.5, "fen": 32.0}, "6": {...}}
    denemeler: Dict[int, SayisalDeneme]
    sinav_tarihi: Optional[str] = "21.06.2025" # GG.AA.YYYY formatında

class TYTRequest(BaseModel):
    denemeler: Dict[int, TYTDeneme]
    sinav_tarihi: Optional[str] = "21.06.2025"

# --- Hafızada Tutulacak Dataset Değişkenleri ---
SAYISAL_DF = None
TYT_DF = None

@app.on_event("startup")
def load_data():
    """Sunucu başlarken CSV/Excel verilerini RAM'e yükleyerek hız kazandırır."""
    global SAYISAL_DF, TYT_DF
    if sayisal is not None:
        print("SAYISAL Veri seti yükleniyor...")
        try:
            SAYISAL_DF = sayisal.veri_yukle()
        except Exception as e:
            print(f"Sayisal Veri Yükleme Hatası: {e}")
            
    if tyt is not None:
        print("TYT Veri seti yükleniyor...")
        try:
            TYT_DF = tyt.veri_yukle()
        except Exception as e:
            print(f"TYT Veri Yükleme Hatası: {e}")

# --- API Uç Noktaları (Endpoints) ---

@app.post("/api/v1/tahmin/ayt-sayisal")
def tahmin_sayisal(req: SayisalRequest):
    """AYT Sayısal denemelerine göre sınav net tahmini yapar."""
    if sayisal is None:
        raise HTTPException(status_code=500, detail="Sayısal modülü yüklenemedi.")
        
    if len(req.denemeler) == 0:
        raise HTTPException(status_code=400, detail="En az 1 dönem (deneme) girmelisiniz.")
        
    try:
        sinav_tarihi_dt = datetime.strptime(req.sinav_tarihi, "%d.%m.%Y")
    except ValueError:
        raise HTTPException(status_code=400, detail="Sınav tarihi GG.AA.YYYY formatında olmalıdır.")

    # Pydantic objelerini, mevcut algoritmaların beklediği saf dict formatına çeviriyoruz.
    denemeler_dict = {k: v.dict() for k, v in req.denemeler.items()}
    
    # 1. Trend Tahmini
    trend_tahmin = None
    if len(denemeler_dict) >= 2:
        try:
            trendler, sirali = sayisal.trend_hesapla(denemeler_dict)
            trend_tahmin = sayisal.sinav_tahmini_hesapla(trendler, sirali, sinav_tarihi_dt)
        except Exception as e:
            print(f"Trend hesaplama hatası: {e}")
            pass
            
    # 2. Makine Öğrenmesi Tahmini
    ml_sonuc = None
    if SAYISAL_DF is not None and len(SAYISAL_DF) > 0:
        try:
            ml_sonuc = sayisal.ml_tahmin(SAYISAL_DF, denemeler_dict)
        except Exception as e:
            print(f"ML tahmin hatası: {e}")
            pass
            
    # 3. Benzer Öğrenci Analizi
    benzerler = None
    if SAYISAL_DF is not None and len(SAYISAL_DF) > 0:
        try:
            raw_benzer = sayisal.benzer_ogrenciler_bul(SAYISAL_DF, denemeler_dict)
            if raw_benzer:
                benzerler = {
                    "benzer_sayisi": raw_benzer.get("benzer_sayisi", 0),
                    "son_sinav_ortalama": raw_benzer.get("son_ortalama"),
                    "ortalama_gelisim": raw_benzer.get("ortalama_gelisim", {}).get("toplam")
                }
        except Exception:
            pass
            
    # 4. Ortalama Hepsaplama (En iyi sonucu vermek için)
    ort = None
    if trend_tahmin and ml_sonuc:
        ort = {
            'matematik': round((trend_tahmin['matematik'] + ml_sonuc['matematik']) / 2, 2),
            'fen': round((trend_tahmin['fen'] + ml_sonuc['fen']) / 2, 2),
            'toplam': round((trend_tahmin['toplam'] + ml_sonuc['toplam']) / 2, 2),
        }
    elif trend_tahmin:
        ort = {k: round(v, 2) for k, v in trend_tahmin.items()}
    elif ml_sonuc:
         ort = {k: round(v, 2) for k, v in ml_sonuc.items()}

    # Sonuçları JSON objesi olarak Framework otomatik dönecektir.
    return {
        "status": "success",
        "data": {
            "ortalama_tahmin": ort,
            "trend_tahmin": {k: round(v, 2) for k, v in trend_tahmin.items()} if trend_tahmin else None,
            "ml_tahmin": {k: round(v, 2) for k, v in ml_sonuc.items()} if ml_sonuc else None,
            "benzer_ogrenci_analizi": benzerler
        }
    }

@app.post("/api/v1/tahmin/tyt")
def tahmin_tyt(req: TYTRequest):
    """TYT denemelerine göre sınav net tahmini yapar."""
    if tyt is None:
        raise HTTPException(status_code=500, detail="TYT modülü yüklenemedi.")
        
    if len(req.denemeler) == 0:
        raise HTTPException(status_code=400, detail="En az 1 dönem (deneme) girmelisiniz.")
        
    try:
        sinav_tarihi_dt = datetime.strptime(req.sinav_tarihi, "%d.%m.%Y")
    except ValueError:
        raise HTTPException(status_code=400, detail="Sınav tarihi GG.AA.YYYY formatında olmalıdır.")

    denemeler_dict = {k: v.dict() for k, v in req.denemeler.items()}
    
    trend_tahmin = None
    if len(denemeler_dict) >= 2:
        try:
            trendler, sirali = tyt.trend_hesapla(denemeler_dict)
            trend_tahmin = tyt.sinav_tahmini_hesapla(trendler, sirali, sinav_tarihi_dt)
        except Exception:
            pass
            
    ml_sonuc = None
    if TYT_DF is not None and len(TYT_DF) > 0:
        try:
            ml_sonuc = tyt.ml_tahmin(TYT_DF, denemeler_dict)
        except Exception:
            pass
            
    benzerler = None
    if TYT_DF is not None and len(TYT_DF) > 0:
        try:
            raw_benzer = tyt.benzer_ogrenciler_bul(TYT_DF, denemeler_dict)
            if raw_benzer:
                benzerler = {
                    "benzer_sayisi": raw_benzer.get("benzer_sayisi", 0),
                    "son_sinav_ortalama": raw_benzer.get("son_ortalama"),
                    "ortalama_gelisim": raw_benzer.get("ortalama_gelisim", {}).get("toplam")
                }
        except Exception:
            pass
            
    ort = None
    if trend_tahmin and ml_sonuc:
        ort = {
            'turkce': round((trend_tahmin['turkce'] + ml_sonuc['turkce']) / 2, 2),
            'matematik': round((trend_tahmin['matematik'] + ml_sonuc['matematik']) / 2, 2),
            'fen': round((trend_tahmin['fen'] + ml_sonuc['fen']) / 2, 2),
            'sosyal': round((trend_tahmin['sosyal'] + ml_sonuc['sosyal']) / 2, 2),
            'toplam': round((trend_tahmin['toplam'] + ml_sonuc['toplam']) / 2, 2),
        }
    elif trend_tahmin:
        ort = {k: round(v, 2) for k, v in trend_tahmin.items()}
    elif ml_sonuc:
        ort = {k: round(v, 2) for k, v in ml_sonuc.items()}

    return {
        "status": "success",
        "data": {
            "ortalama_tahmin": ort,
            "trend_tahmin": {k: round(v, 2) for k, v in trend_tahmin.items()} if trend_tahmin else None,
            "ml_tahmin": {k: round(v, 2) for k, v in ml_sonuc.items()} if ml_sonuc else None,
            "benzer_ogrenci_analizi": benzerler
        }
    }

if __name__ == "__main__":
    import uvicorn
    # Standart uvicorn çalıştırıcı, debug modunda localhost:8000 portundan dinler
    uvicorn.run("api:app", host="0.0.0.0", port=8000, reload=True)

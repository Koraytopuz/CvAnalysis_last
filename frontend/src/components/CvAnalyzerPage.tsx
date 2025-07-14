import React, { useState, useCallback, useEffect } from 'react';
import { useDropzone } from 'react-dropzone';
import axios from 'axios';

// Material-UI importları...
import {
    Box,
    Card,
    CardContent,
    Typography,
    CircularProgress,
    Chip,
    Alert,
    TextField
} from '@mui/material';
import UploadFileIcon from '@mui/icons-material/UploadFile';
import WarningAmberIcon from '@mui/icons-material/WarningAmber';
import TrendingUpIcon from '@mui/icons-material/TrendingUp';
import KeyIcon from '@mui/icons-material/VpnKey';
import LightbulbIcon from '@mui/icons-material/Lightbulb';
import FiberManualRecordIcon from '@mui/icons-material/FiberManualRecord';
import { ThemeProvider, createTheme } from '@mui/material/styles';

interface AnalysisReport {
    atsScore: number;
    contentScore: number;
    foundKeywords: string[];
    missingKeywords: string[];
    positiveFeedback: string[];
    suggestions: string[];
    extractedCvText: string;
    atsImprovementTips: string[];
    extraAdvice: string[];
    atsScoreDetails?: string; // Added for new detail display
}

const CircularProgressWithLabel = (props: { value: number }) => (
    <Box sx={{ position: 'relative', display: 'inline-flex' }}>
        <CircularProgress variant="determinate" {...props} size={120} thickness={4} />
        <Box
            sx={{
                top: 0,
                left: 0,
                bottom: 0,
                right: 0,
                position: 'absolute',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
            }}
        >
            <Typography variant="h4" component="div" color="primary.main" sx={{ fontWeight: 'bold' }}>
                {`${Math.round(props.value)}`}
                <Typography variant="h6" component="span" color="text.secondary">/100</Typography>
            </Typography>
        </Box>
    </Box>
);

export const CvAnalyzerPage: React.FC = () => {
    const [jobDescription, setJobDescription] = useState<string>('');
    const [analysisReport, setAnalysisReport] = useState<AnalysisReport | null>(null);
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [error, setError] = useState<string | null>(null);
    const theme = createTheme({
      palette: {
        mode: 'light', // Always light mode
        primary: { main: '#6366f1' },
      },
    });

    useEffect(() => {
      const onScroll = () => {
        if (window.scrollY === 0) {
          // setShowLangBox(true); // Removed language selection UI
        } else {
          // setShowLangBox(false); // Removed language selection UI
        }
      };
      window.addEventListener('scroll', onScroll);
      return () => window.removeEventListener('scroll', onScroll);
    }, []);

    const onDrop = useCallback(async (acceptedFiles: File[]) => {
        const file = acceptedFiles[0];
        if (!file) return;

        setIsLoading(true);
        setError(null);
        setAnalysisReport(null);

        const formData = new FormData();
        formData.append('file', file);
        formData.append('jobDescription', jobDescription || '');
        // formData.append('lang', lang); // Removed lang from formData

        try {
            // Development için localhost, production için Render URL
            const API_BASE_URL = import.meta.env.PROD 
                ? 'https://cvanalysis-last.onrender.com'
                : 'http://localhost:5191';
            
            const response = await axios.post<AnalysisReport>(
                `${API_BASE_URL}/api/CvAnalysis/upload`, 
                formData,
                {
                    headers: {
                        'Content-Type': 'multipart/form-data',
                    },
                    timeout: 30000, // 30 saniye timeout
                }
            );
            
            setAnalysisReport(response.data);
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        } catch (err: any) {
            console.error('Detailed error:', err);
            
            if (err.response) {
                // Server responded with error
                console.error('Error response:', err.response.data);
                setError(`Server hatası: ${err.response.data.error || err.response.data.message || 'Bilinmeyen hata'}`);
            } else if (err.request) {
                // Request was made but no response received
                console.error('Network error:', err.request);
                setError('Sunucuya bağlanılamadı. Lütfen backend servisinin çalıştığından emin olun.');
            } else {
                // Something else happened
                console.error('Error message:', err.message);
                setError(`Beklenmeyen hata: ${err.message}`);
            }
        } finally {
            setIsLoading(false);
        }
    }, [jobDescription]); // Removed lang from dependency array

    const { getRootProps, getInputProps, isDragActive } = useDropzone({
        onDrop,
        accept: {
            'application/pdf': ['.pdf'],
            'application/vnd.openxmlformats-officedocument.wordprocessingml.document': ['.docx']
        },
        maxFiles: 1,
        maxSize: 10 * 1024 * 1024, // 10MB limit
    });

    // Test backend connection
    const testConnection = async () => {
        try {
            const API_BASE_URL = import.meta.env.PROD 
                ? 'https://cvanalysis-last.onrender.com'
                : 'http://localhost:5191';
            const response = await axios.get(`${API_BASE_URL}/api/CvAnalysis/test`);
            console.log('Test response:', response.data);
        } catch (err) {
            console.error('Test failed:', err);
        }
    };

    React.useEffect(() => {
        testConnection();
    }, []);

    return (
        <ThemeProvider theme={theme}>
            <>
                {/* Sağ üstte sabit dil seçici kutusu, ThemeProvider'ın da dışında */}
                {/* Select ve FormControl'u kaldır, sadece bayrak img ve Box kullan */}
                <Box
                    sx={{
                        position: 'fixed',
                        top: { xs: 10, md: 18 },
                        right: { xs: 10, md: 24 },
                        zIndex: 3000,
                        bgcolor: 'rgba(255,255,255,0.7)',
                        backdropFilter: 'blur(8px)',
                        borderRadius: 6,
                        boxShadow: 8,
                        border: '2px solid #6366f1',
                        width: 60,
                        height: 30,
                        px: 0,
                        py: 0,
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        minWidth: 0,
                        gap: 0,
                        pointerEvents: 'none', // Removed language selection UI
                        opacity: 0, // Removed language selection UI
                        transition: 'opacity 0.3s',
                        overflow: 'hidden',
                        cursor: 'pointer',
                    }}
                    // Removed language toggle onClick
                >
                    <img
                        src="https://upload.wikimedia.org/wikipedia/commons/b/b4/Flag_of_Turkey.svg" // Always Turkish flag
                        alt="TR"
                        style={{
                            width: '100%',
                            height: '100%',
                            objectFit: 'cover',
                            display: 'block',
                            margin: 0,
                            borderRadius: 0,
                            background: 'transparent',
                        }}
                    />
                </Box>
                {/* Hero alanı */}
                <Box sx={{
                    width: '100%',
                    minHeight: '100vh',
                    background: 'linear-gradient(135deg, #e0e7ff 0%, #f4f6f8 100%)',
                }}>
                    <Box sx={{
                        py: { xs: 2, md: 4 }, // Daha az üst-alt boşluk
                        mb: 4,
                        textAlign: 'center',
                    }}>
                        <Typography variant="h2" component="h1" sx={{ fontWeight: 'bold', letterSpacing: 1, color: '#6366f1', mb: 2, fontSize: { xs: 32, md: 48 } }}>
                            {/* Removed t() for language selection */}
                            Yapay Zeka Destekli CV Analiz Motoru
                        </Typography>
                        <Typography variant="h5" sx={{ color: '#3b3b3b', mb: 2, fontWeight: 500 }}>
                            {/* Removed t() for language selection */}
                            CV'nizi yükleyerek ATS uyumluluğunu ve içerik kalitesini saniyeler içinde ölçün.
                        </Typography>
                    </Box>
                    {/* Ana içerik kutusu */}
                    <Box sx={{
                        p: { xs: 1, md: 4 },
                        maxWidth: '1400px',
                        margin: 'auto',
                        position: 'relative',
                        transition: 'background 0.5s',
                    }}>
                        {/* 1. ve 2. adım kutuları için ayrı grid */}
                        <Box
                          sx={{
                            display: 'grid',
                            gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' },
                            gap: { xs: 3, md: 6 },
                            mb: 0,
                          }}
                        >
                          {/* İş İlanı Metin Kutusu */}
                          <Card
                            elevation={0}
                            sx={{
                              borderRadius: 4,
                              boxShadow: 'none',
                              background: 'rgba(255,255,255,0.85)',
                              border: '1.5px solid #e0e7ff',
                              p: 4,
                              backdropFilter: 'blur(2px)',
                              transition: 'box-shadow 0.3s, transform 0.2s',
                              '&:hover': {
                                boxShadow: '0 2px 12px 0 rgba(99,102,241,0.10)',
                                transform: 'scale(1.01)'
                              },
                              height: '100%'
                            }}
                          >
                            <CardContent>
                              <Typography
                                variant="h6"
                                gutterBottom
                                sx={{
                                  fontWeight: 700,
                                  color: '#3f51b5',
                                  fontSize: { xs: 18, md: 22 },
                                  mb: 2
                                }}
                              >
                                {/* Removed t() for language selection */}
                                Adım 1: İş İlanı Metnini Yapıştırın (İsteğe Bağlı)
                              </Typography>
                              <TextField
                                className="custom-scroll"
                                fullWidth
                                multiline
                                rows={8}
                                variant="outlined"
                                label="İş İlanı Metni"
                                placeholder="Karşılaştırma yapmak istediğiniz iş ilanı metnini buraya yapıştırın..."
                                value={jobDescription}
                                onChange={(e) => setJobDescription(e.target.value)}
                                sx={{
                                  background: '#fff',
                                  borderRadius: 2,
                                  '& .MuiOutlinedInput-root': {
                                    borderRadius: 2,
                                  },
                                  '& .MuiOutlinedInput-notchedOutline': {
                                    borderColor: '#c7d2fe'
                                  },
                                  '&:hover .MuiOutlinedInput-notchedOutline': {
                                    borderColor: '#6366f1'
                                  },
                                  '& .MuiInputLabel-root': {
                                    color: '#6366f1'
                                  }
                                }}
                                InputProps={{
                                  style: { fontSize: 16, color: '#22223b' }
                                }}
                              />
                            </CardContent>
                          </Card>

                          {/* CV Yükleme Alanı */}
                          <Card
                            elevation={0}
                            sx={{
                              borderRadius: 4,
                              boxShadow: 'none',
                              background: 'rgba(255,255,255,0.85)',
                              border: '1.5px solid #e0e7ff',
                              p: 4,
                              backdropFilter: 'blur(2px)',
                              transition: 'box-shadow 0.3s, transform 0.2s',
                              '&:hover': {
                                boxShadow: '0 2px 12px 0 rgba(99,102,241,0.10)',
                                transform: 'scale(1.01)'
                              },
                              height: '100%'
                            }}
                          >
                            <CardContent>
                              <Typography variant="h6" gutterBottom sx={{ fontWeight: 700, color: '#3f51b5', fontSize: { xs: 18, md: 22 }, mb: 2 }}>Adım 2: CV'nizi Yükleyin ve Analiz Edin</Typography>
                              <Box {...getRootProps()} sx={{ 
                                  border: `3px dashed ${isDragActive ? '#3f51b5' : 'lightgrey'}`, 
                                  p: 4, 
                                  textAlign: 'center', 
                                  cursor: 'pointer', 
                                  mt: 2, 
                                  borderRadius: 2, 
                                  bgcolor: isDragActive ? '#e8eaf6' : 'white', 
                                  transition: 'background-color 0.3s ease', 
                                  height: '200px',
                                  display: 'flex',
                                  flexDirection: 'column',
                                  alignItems: 'center',
                                  justifyContent: 'center'
                              }}>
                                  <input {...getInputProps()} />
                                  <UploadFileIcon sx={{ fontSize: 40, color: 'grey.500', mb: 1 }} />
                                  <Typography sx={{ fontWeight: 500, mb: 0.5 }}>{isDragActive ? 'Dosyayı Buraya Bırakın...' : 'Analiz İçin CV Dosyanızı Sürükleyin'}</Typography>
                                  <Typography color="text.secondary" variant="body2">veya seçmek için tıklayın (.pdf, .docx)
                                  </Typography>
                              </Box>
                            </CardContent>
                          </Card>
                        </Box>

                        {/* Sonuç kartları: analiz sonrası ayrı grid ve üstte belirgin boşluk */}
                        {analysisReport && (
                          <Box
                            sx={{
                              display: 'grid',
                              gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' },
                              gap: { xs: 3, md: 6 },
                              mt: { xs: 6, md: 8 }, // Üstte belirgin boşluk
                              mb: 0,
                            }}
                          >
                            {/* ATS Uyumluluk Puanı */}
                            <Card elevation={0} sx={{ borderRadius: 4, boxShadow: 'none', background: 'rgba(255,255,255,0.85)', border: '1.5px solid #e0e7ff', p: 5, minHeight: 220, display: 'flex', flexDirection: 'column', justifyContent: 'center', backdropFilter: 'blur(2px)', transition: 'box-shadow 0.3s, transform 0.2s', '&:hover': { boxShadow: '0 2px 12px 0 rgba(99,102,241,0.10)', transform: 'scale(1.01)' } }}>
                              <CardContent sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100%' }}>
                                  <TrendingUpIcon sx={{ fontSize: 36, color: '#6366f1', mb: 1 }} />
                                  <Typography variant="h5" component="div" gutterBottom sx={{ fontWeight: '600' }}>
                                      ATS Uyumluluk Puanı
                                  </Typography>
                                  <CircularProgressWithLabel value={analysisReport.atsScore} />
                                  {/* Puanlama detayları */}
                                  {analysisReport.atsScoreDetails && (
                                    <Box
                                      sx={{
                                        mt: 2,
                                        width: '100%',
                                        maxWidth: 420,
                                        textAlign: 'left',
                                        bgcolor: '#f5f7ff',
                                        border: '1.5px solid #e0e7ff',
                                        borderRadius: 3,
                                        boxShadow: 2,
                                        px: 3,
                                        py: 2.5,
                                        display: 'flex',
                                        flexDirection: 'column',
                                        alignItems: 'flex-start',
                                        gap: 1.5,
                                      }}
                                    >
                                      <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                                        <TrendingUpIcon sx={{ fontSize: 22, color: '#6366f1', mr: 1 }} />
                                        <Typography variant="subtitle1" sx={{ fontWeight: 700, color: '#6366f1' }}>
                                          Puanlama Sonucu
                                        </Typography>
                                      </Box>
                                      {(() => {
                                        const details = analysisReport.atsScoreDetails.split('\n');
                                        const fields = [
                                          { label: 'Okunabilirlik', key: 'Format ve Okunabilirlik' },
                                          { label: 'Anahtar Kelime Optimizasyonu', key: 'Anahtar Kelime Optimizasyonu' },
                                          { label: 'İçerik ve Yapı', key: 'İçerik ve Yapı' },
                                          { label: 'Genel Kalite ve Profesyonellik', key: 'Genel Kalite ve Profesyonellik' },
                                        ];
                                        const items = fields.map((field, idx) => {
                                          const line = details.find(l => l.includes(field.key));
                                          let value = '';
                                          if (line) {
                                            const match = line.match(/([0-9]+\/[0-9]+)/);
                                            if (match) value = match[1];
                                          }
                                          return (
                                            <Box key={idx} sx={{ display: 'flex', alignItems: 'center', gap: 1.2 }}>
                                              <FiberManualRecordIcon sx={{ fontSize: 12, color: '#6366f1', mr: 1 }} />
                                              <Typography variant="body1" sx={{ fontWeight: 500, color: '#22223b' }}>
                                                {field.label}: {value}
                                              </Typography>
                                            </Box>
                                          );
                                        });
                                        return <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5, width: '100%' }}>{items}</Box>;
                                      })()}
                                    </Box>
                                  )}
                              </CardContent>
                            </Card>
                            {/* ATS Puanını Yükseltmek İçin Tavsiyeler */}
                            <Card elevation={0} sx={{ borderRadius: 4, boxShadow: 'none', background: 'rgba(255,255,255,0.85)', border: '1.5px solid #e0e7ff', p: 5, minHeight: 220, display: 'flex', flexDirection: 'column', justifyContent: 'center', backdropFilter: 'blur(2px)', transition: 'box-shadow 0.3s, transform 0.2s', '&:hover': { boxShadow: '0 2px 12px 0 rgba(99,102,241,0.10)', transform: 'scale(1.01)' } }}>
                              <CardContent sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100%' }}>
                                  <TrendingUpIcon sx={{ fontSize: 28, color: '#f59e42', mb: 1 }} />
                                  <Typography variant="h6" gutterBottom>ATS Puanını Yükseltmek İçin Tavsiyeler</Typography>
                                  <ul style={{ marginTop: 8, marginBottom: 0, textAlign: 'left' }}>
                                    {(() => {
                                      const tips = analysisReport.atsImprovementTips && analysisReport.atsImprovementTips.length > 0
                                        ? analysisReport.atsImprovementTips.slice(0, 6)
                                        : [];
                                      const tipsToShow = tips.length < 6
                                        ? [...tips, ...Array(6 - tips.length).fill('Daha fazla öneri üretilemedi.')]
                                        : tips;
                                      return tipsToShow.map((tip, i) => (
                                        <li key={i} style={{ marginBottom: 6 }}>
                                          <Typography variant="body2">{tip}</Typography>
                                        </li>
                                      ));
                                    })()}
                                  </ul>
                              </CardContent>
                            </Card>
                            {/* Bulunan Anahtar Kelimeler */}
                            <Card elevation={0} sx={{ borderRadius: 4, boxShadow: 'none', background: 'rgba(255,255,255,0.85)', border: '1.5px solid #e0e7ff', p: 5, minHeight: 220, display: 'flex', flexDirection: 'column', justifyContent: 'center', backdropFilter: 'blur(2px)', transition: 'box-shadow 0.3s, transform 0.2s', '&:hover': { boxShadow: '0 2px 12px 0 rgba(99,102,241,0.10)', transform: 'scale(1.01)' } }}>
                              <CardContent sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100%' }}>
                                  <KeyIcon sx={{ fontSize: 28, color: '#6366f1', mb: 1 }} />
                                  <Typography variant="h6">Bulunan Anahtar Kelimeler</Typography>
                                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mt: 2, justifyContent: 'center' }}>
                                      {analysisReport.foundKeywords.length > 0 ? 
                                          analysisReport.foundKeywords.map(keyword => (
                                              <Chip key={keyword} label={keyword} color="success" variant="filled" />
                                          )) : 
                                          <Typography variant="body2" color="text.secondary">
                                              CV’nizde iş ilanındaki anahtar kelimelerden hiçbiri bulunamadı. CV’nizi güncelleyebilirsiniz.
                                          </Typography>
                                      }
                                  </Box>
                              </CardContent>
                            </Card>
                            {/* Eksik Anahtar Kelimeler */}
                            <Card elevation={0} sx={{ borderRadius: 4, boxShadow: 'none', background: 'rgba(255,255,255,0.85)', border: '1.5px solid #e0e7ff', p: 5, minHeight: 220, display: 'flex', flexDirection: 'column', justifyContent: 'center', backdropFilter: 'blur(2px)', transition: 'box-shadow 0.3s, transform 0.2s', '&:hover': { boxShadow: '0 2px 12px 0 rgba(99,102,241,0.10)', transform: 'scale(1.01)' } }}>
                              <CardContent sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100%' }}>
                                  <WarningAmberIcon sx={{ fontSize: 28, color: '#f59e42', mb: 1 }} />
                                  <Typography variant="h6">Eksik Anahtar Kelimeler</Typography>
                                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mt: 2, justifyContent: 'center' }}>
                                      {analysisReport.missingKeywords.length > 0 ? 
                                          analysisReport.missingKeywords.map(keyword => (
                                              <Chip key={keyword} label={keyword} color="warning" variant="outlined" />
                                          )) : 
                                          <Typography variant="body2" color="text.secondary">
                                              Tebrikler! Hedeflenen tüm anahtar kelimeler CV’nizde mevcut.
                                          </Typography>
                                      }
                                  </Box>
                              </CardContent>
                            </Card>
                          </Box>
                        )}
                        {/* Ekstra Tavsiyeler kartı */}
                        {analysisReport && analysisReport.extraAdvice && analysisReport.extraAdvice.length > 0 && (
                          <Box
                            sx={{
                              mt: { xs: 3, md: 4 },
                              display: 'flex',
                              flexDirection: 'row',
                              justifyContent: 'center',
                            }}
                          >
                            <Card elevation={0} sx={{ maxWidth: 900, width: '100%', borderRadius: 4, boxShadow: 'none', background: 'rgba(255,255,255,0.95)', border: '1.5px solid #e0e7ff', p: 5, minHeight: 180, display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center', mx: 'auto', backdropFilter: 'blur(2px)', transition: 'box-shadow 0.3s, transform 0.2s', '&:hover': { boxShadow: '0 2px 12px 0 rgba(99,102,241,0.10)', transform: 'scale(1.01)' } }}>
                              <CardContent sx={{ width: '100%', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100%' }}>
                                <LightbulbIcon sx={{ fontSize: 32, color: '#f59e42', mb: 1 }} />
                                <Typography variant="h6" gutterBottom sx={{ fontWeight: 700, color: '#f59e42', mb: 2 }}>Ekstra Tavsiyeler</Typography>
                                <ul style={{ marginTop: 8, marginBottom: 0, textAlign: 'left', maxWidth: 700 }}>
                                  {analysisReport.extraAdvice.map((advice, i) => (
                                    <li key={i} style={{ marginBottom: 6 }}>
                                      <Typography variant="body2">{advice}</Typography>
                                    </li>
                                  ))}
                                </ul>
                              </CardContent>
                            </Card>
                          </Box>
                        )}
                        {/* Yükleniyor, hata ve diğer durumlar için grid dışında gösterim devam edecek */}
                        {isLoading && (
                            <Box sx={{
                              display: 'flex',
                              flexDirection: 'column',
                              alignItems: 'center',
                              justifyContent: 'center',
                              minHeight: 320,
                              py: 8,
                              mt: 6
                            }}>
                              <CircularProgress size={64} thickness={5} color="primary" />
                              <Typography variant="h6" sx={{ mt: 3, color: '#6366f1', fontWeight: 600 }}>
                                CV’niz analiz ediliyor, lütfen bekleyin...
                              </Typography>
                            </Box>
                        )}
                        {error && <Alert severity="error" variant="filled" sx={{ mb: 3 }}>{error}</Alert>}
                    </Box>
                </Box>
            </>
        </ThemeProvider>
    );
};
import { useState, useEffect } from 'react';
import axios from 'axios';
import { Search, RefreshCw, AlertCircle, Info, Building2, User, Scale, Download } from 'lucide-react';
import './App.css';

const API_BASE_URL = 'http://localhost:5198/api/ecourt';

function App() {
  const [captchaBase64, setCaptchaBase64] = useState('');
  const [sessionId, setSessionId] = useState('');
  const [cnrNumber, setCnrNumber] = useState('');
  const [captchaText, setCaptchaText] = useState('');
  
  const [loadingCaptcha, setLoadingCaptcha] = useState(false);
  const [searching, setSearching] = useState(false);
  const [exportingPdf, setExportingPdf] = useState(false);
  const [error, setError] = useState('');
  const [caseDetails, setCaseDetails] = useState(null);

  const handleExportPdf = async () => {
    if (!caseDetails || !caseDetails.cnrNumber) return;
    try {
      setExportingPdf(true);
      const url = `${API_BASE_URL}/export-pdf/${caseDetails.cnrNumber}`;
      const response = await fetch(url);
      
      if (!response.ok) {
        throw new Error('Failed to generate case report PDF.');
      }
      
      const blob = await response.blob();
      const downloadUrl = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = downloadUrl;
      link.setAttribute('download', `CaseReport_${caseDetails.cnrNumber}.pdf`);
      document.body.appendChild(link);
      link.click();
      link.parentNode.removeChild(link);
    } catch (err) {
      console.error(err);
      alert('Failed to download PDF case report. Please make sure the backend is running.');
    } finally {
      setExportingPdf(false);
    }
  };

  const fetchCaptcha = async () => {
    try {
      setLoadingCaptcha(true);
      setError('');
      const response = await axios.get(`${API_BASE_URL}/captcha`);
      setCaptchaBase64(response.data.captchaBase64);
      setSessionId(response.data.sessionId);
      setCaptchaText(''); // clear previous input
    } catch (err) {
      console.error(err);
      setError('Failed to load CAPTCHA. The eCourts website might be down.');
    } finally {
      setLoadingCaptcha(false);
    }
  };

  useEffect(() => {
    fetchCaptcha();
  }, []);

  const handleSearch = async (e) => {
    e.preventDefault();
    if (!cnrNumber || !captchaText) {
      setError('Please enter both CNR Number and CAPTCHA');
      return;
    }
    
    if (cnrNumber.length !== 16) {
      setError('CNR Number must be exactly 16 characters');
      return;
    }

    try {
      setSearching(true);
      setError('');
      setCaseDetails(null);
      
      const response = await axios.post(`${API_BASE_URL}/search`, {
        sessionId,
        cnrNumber,
        captcha: captchaText
      });

      if (response.data.success && response.data.caseDetails) {
        setCaseDetails(response.data.caseDetails);
      } else {
        setError(response.data.message || 'Failed to extract case details.');
        // Need a new captcha after a failed attempt since session is closed
        fetchCaptcha();
      }
    } catch (err) {
      console.error(err);
      setError(err.response?.data?.message || 'An error occurred while searching.');
      fetchCaptcha();
    } finally {
      setSearching(false);
    }
  };

  return (
    <div className="container">
      <header className="app-header">
        <h1 className="app-title">eCourts Extractor</h1>
        <p className="app-subtitle">Retrieve case data instantly using CNR number</p>
      </header>

      <main>
        {!caseDetails && (
          <div className="card search-form">
            {error && (
              <div className="alert alert-error">
                <AlertCircle size={20} />
                <span>{error}</span>
              </div>
            )}
            
            <form onSubmit={handleSearch} className="form-group">
              <div className="form-group">
                <label htmlFor="cnr">CNR Number (16 Digits)</label>
                <input
                  id="cnr"
                  type="text"
                  placeholder="e.g. TN12345678901234"
                  value={cnrNumber}
                  onChange={(e) => setCnrNumber(e.target.value.toUpperCase())}
                  maxLength={16}
                  disabled={searching}
                />
              </div>

              <div className="form-group">
                <label>Verification</label>
                <div className="captcha-container">
                  {loadingCaptcha ? (
                    <div className="spin"><RefreshCw size={24} /></div>
                  ) : captchaBase64 ? (
                    <img src={`data:image/png;base64,${captchaBase64}`} alt="CAPTCHA" className="captcha-image" />
                  ) : (
                    <span className="text-muted">No Captcha</span>
                  )}
                  <button type="button" className="reload-btn" onClick={fetchCaptcha} disabled={loadingCaptcha || searching}>
                    <RefreshCw size={20} className={loadingCaptcha ? 'spin' : ''} />
                  </button>
                </div>
              </div>

              <div className="form-group">
                <label htmlFor="captcha">Enter CAPTCHA Text</label>
                <input
                  id="captcha"
                  type="text"
                  placeholder="Enter characters shown above"
                  value={captchaText}
                  onChange={(e) => setCaptchaText(e.target.value)}
                  disabled={searching || loadingCaptcha}
                />
              </div>

              <button type="submit" className="btn-primary" disabled={searching || loadingCaptcha}>
                {searching ? <RefreshCw size={20} className="spin" /> : <Search size={20} />}
                {searching ? 'Extracting Data...' : 'Search Case'}
              </button>
            </form>
          </div>
        )}

        {caseDetails && (
          <div className="results-container">
            <div className="flex" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
               <h2 style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                 <Building2 size={24} className="text-primary" /> Case Details
               </h2>
               <div style={{ display: 'flex', gap: '0.75rem' }}>
                 <button onClick={handleExportPdf} className="btn-secondary" style={{ padding: '0.5rem 1rem' }} disabled={exportingPdf}>
                   {exportingPdf ? <RefreshCw size={16} className="spin" /> : <Download size={16} />}
                   {exportingPdf ? 'Exporting...' : 'Export PDF'}
                 </button>
                 <button onClick={() => { setCaseDetails(null); fetchCaptcha(); }} className="btn-primary" style={{ padding: '0.5rem 1rem' }} disabled={exportingPdf}>
                   New Search
                 </button>
               </div>
            </div>
            
            <div className="grid-2">
              <div className="card">
                <h3 style={{ marginBottom: '1rem', borderBottom: '1px solid var(--border-color)', paddingBottom: '0.5rem' }}>Basic Information</h3>
                <div className="detail-row">
                  <span className="detail-label">CNR Number</span>
                  <span className="detail-value text-primary">{caseDetails.cnrNumber}</span>
                </div>
                <div className="detail-row">
                  <span className="detail-label">Case Type</span>
                  <span className="detail-value">{caseDetails.caseType || 'N/A'}</span>
                </div>
                <div className="detail-row">
                  <span className="detail-label">Status</span>
                  <span className="detail-value" style={{ color: 'var(--success-color)' }}>{caseDetails.caseStatus || 'N/A'}</span>
                </div>
                <div className="detail-row">
                  <span className="detail-label">Court Establishment</span>
                  <span className="detail-value">{caseDetails.courtEstablishment || 'N/A'}</span>
                </div>
              </div>

              <div className="card">
                <h3 style={{ marginBottom: '1rem', borderBottom: '1px solid var(--border-color)', paddingBottom: '0.5rem' }}>Dates</h3>
                <div className="detail-row">
                  <span className="detail-label">Filing Date</span>
                  <span className="detail-value">{caseDetails.filingDate || 'N/A'}</span>
                </div>
                <div className="detail-row">
                  <span className="detail-label">Registration Date</span>
                  <span className="detail-value">{caseDetails.registrationDate || 'N/A'}</span>
                </div>
                <div className="detail-row">
                  <span className="detail-label">First Hearing</span>
                  <span className="detail-value">{caseDetails.firstHearingDate || 'N/A'}</span>
                </div>
                <div className="detail-row">
                  <span className="detail-label">Next Hearing</span>
                  <span className="detail-value">{caseDetails.nextHearingDate || 'N/A'}</span>
                </div>
              </div>
            </div>

            <div className="card">
              <h3 style={{ marginBottom: '1rem', borderBottom: '1px solid var(--border-color)', paddingBottom: '0.5rem' }}>Parties</h3>
              <div className="grid-2">
                <div>
                  <div className="detail-label" style={{ display: 'flex', alignItems: 'center', gap: '0.25rem' }}><User size={14}/> Petitioners</div>
                  {caseDetails.petitioners && caseDetails.petitioners.length > 0 ? (
                    <ol style={{ paddingLeft: '1.2rem', marginTop: '0.5rem', color: 'var(--text-color)' }}>
                      {caseDetails.petitioners.map((p, idx) => <li key={idx} style={{ marginBottom: '0.25rem' }}>{p}</li>)}
                    </ol>
                  ) : <div className="detail-value">N/A</div>}
                  {caseDetails.petitionerAdvocate && <div className="text-muted" style={{ fontSize: '0.875rem', marginTop: '0.5rem', paddingLeft: '0.2rem' }}>Advocate: {caseDetails.petitionerAdvocate}</div>}
                </div>
                <div>
                  <div className="detail-label" style={{ display: 'flex', alignItems: 'center', gap: '0.25rem' }}><User size={14}/> Respondents</div>
                  {caseDetails.respondents && caseDetails.respondents.length > 0 ? (
                    <ol style={{ paddingLeft: '1.2rem', marginTop: '0.5rem', color: 'var(--text-color)' }}>
                      {caseDetails.respondents.map((r, idx) => <li key={idx} style={{ marginBottom: '0.25rem' }}>{r}</li>)}
                    </ol>
                  ) : <div className="detail-value">N/A</div>}
                  {caseDetails.respondentAdvocate && <div className="text-muted" style={{ fontSize: '0.875rem', marginTop: '0.5rem', paddingLeft: '0.2rem' }}>Advocate: {caseDetails.respondentAdvocate}</div>}
                </div>
              </div>
            </div>

            {caseDetails.hearings && caseDetails.hearings.length > 0 && (
              <div className="card" style={{ overflowX: 'auto' }}>
                <h3 style={{ marginBottom: '1rem' }}>Hearing History</h3>
                <table>
                  <thead>
                    <tr>
                      <th>Judge</th>
                      <th>Business On</th>
                      <th>Hearing Date</th>
                      <th>Purpose</th>
                    </tr>
                  </thead>
                  <tbody>
                    {caseDetails.hearings.map((h, i) => (
                      <tr key={i}>
                        <td>{h.judge}</td>
                        <td>{h.businessOnDate}</td>
                        <td>{h.hearingDate}</td>
                        <td>{h.purpose}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
            
            {caseDetails.acts && caseDetails.acts.length > 0 && (
              <div className="card">
                <h3 style={{ marginBottom: '1rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}><Scale size={18} /> Acts & Sections</h3>
                <ul style={{ listStylePosition: 'inside', color: 'var(--text-muted)' }}>
                  {caseDetails.acts.map((act, i) => (
                    <li key={i} style={{ marginBottom: '0.5rem' }}>{act}</li>
                  ))}
                </ul>
              </div>
            )}

            {caseDetails.transferDetails && caseDetails.transferDetails.length > 0 && (
              <div className="card" style={{ overflowX: 'auto' }}>
                <h3 style={{ marginBottom: '1rem' }}>Case Transfer Details</h3>
                <table>
                  <thead>
                    <tr>
                      <th>Registration Number</th>
                      <th>Transfer Date</th>
                      <th>From Court</th>
                      <th>To Court</th>
                    </tr>
                  </thead>
                  <tbody>
                    {caseDetails.transferDetails.map((t, i) => (
                      <tr key={i}>
                        <td>{t.registrationNumber}</td>
                        <td>{t.transferDate}</td>
                        <td>{t.fromCourt}</td>
                        <td>{t.toCourt}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            {caseDetails.iaStatuses && caseDetails.iaStatuses.length > 0 && (
              <div className="card" style={{ overflowX: 'auto' }}>
                <h3 style={{ marginBottom: '1rem' }}>IA Status</h3>
                <table>
                  <thead>
                    <tr>
                      <th>IA Number</th>
                      <th>Party Name</th>
                      <th>Date of Filing</th>
                      <th>Next Date</th>
                      <th>Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {caseDetails.iaStatuses.map((ia, i) => (
                      <tr key={i}>
                        <td>{ia.iaNumber}</td>
                        <td>{ia.partyName}</td>
                        <td>{ia.filingDate}</td>
                        <td>{ia.nextDate}</td>
                        <td>{ia.status}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            {caseDetails.processes && caseDetails.processes.length > 0 && (
              <div className="card" style={{ overflowX: 'auto' }}>
                <h3 style={{ marginBottom: '1rem' }}>Processes</h3>
                <table>
                  <thead>
                    <tr>
                      <th>Process ID</th>
                      <th>Process Title</th>
                      <th>Process Date</th>
                    </tr>
                  </thead>
                  <tbody>
                    {caseDetails.processes.map((p, i) => (
                      <tr key={i}>
                        <td>{p.processId}</td>
                        <td>{p.title}</td>
                        <td>{p.date}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        )}
      </main>
    </div>
  );
}

export default App;

import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import axios from 'axios';
import { ArrowLeft, Search, RefreshCw, ServerCrash, Calendar, FileText, User, Scale, Download, X, Layers } from 'lucide-react';
import './dashboard.css';

const API_BASE_URL = 'http://localhost:5198/api/ecourt';

export default function DatabasePage() {
  const [cases, setCases] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCase, setSelectedCase] = useState(null);
  const [exportingPdf, setExportingPdf] = useState(false);

  const fetchCases = async () => {
    try {
      setLoading(true);
      setError('');
      const response = await axios.get(`${API_BASE_URL}/cases`);
      setCases(response.data || []);
    } catch (err) {
      console.error(err);
      setError('Could not establish a database connection. Ensure your PostgreSQL server is active.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchCases();
  }, []);

  const handleExportPdf = async (cnr) => {
    try {
      setExportingPdf(true);
      const url = `${API_BASE_URL}/export-pdf/${cnr}`;
      const response = await fetch(url);
      
      if (!response.ok) {
        throw new Error('Failed to generate case report PDF.');
      }
      
      const blob = await response.blob();
      const downloadUrl = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = downloadUrl;
      link.setAttribute('download', `CaseReport_${cnr}.pdf`);
      document.body.appendChild(link);
      link.click();
      link.parentNode.removeChild(link);
    } catch (err) {
      console.error(err);
      alert('Failed to download PDF report. Ensure the backend is active.');
    } finally {
      setExportingPdf(false);
    }
  };

  // Filter cases based on search term
  const filteredCases = cases.filter((c) => {
    const term = searchTerm.toLowerCase();
    const cnr = (c.cnrNumber || '').toLowerCase();
    const status = (c.caseStatus || '').toLowerCase();
    const type = (c.caseType || '').toLowerCase();
    
    // Parse party names to make them searchable
    const petitioners = (c.petitioners || []).join(' ').toLowerCase();
    const respondents = (c.respondents || []).join(' ').toLowerCase();

    return cnr.includes(term) || status.includes(term) || type.includes(term) || petitioners.includes(term) || respondents.includes(term);
  });

  // Calculate quick stats
  const totalCases = cases.length;
  const activeCases = cases.filter((c) => (c.caseStatus || '').toLowerCase().includes('active') || (c.caseStatus || '').toLowerCase().includes('pending')).length;
  const disposedCases = totalCases - activeCases;

  return (
    <div className="dashboard-canvas">
      <div className="dashboard-container">
        
        {/* Navigation & Header */}
        <div className="dashboard-nav">
          <Link to="/" className="back-home-link">
            <ArrowLeft size={18} /> Back to Dashboard
          </Link>
          <button onClick={fetchCases} className="btn-secondary" style={{ padding: '0.5rem 1rem', fontSize: '0.85rem' }} disabled={loading}>
            <RefreshCw size={14} className={loading ? 'spin' : ''} /> Refresh Data
          </button>
        </div>

        <div style={{ marginBottom: '2.5rem' }}>
          <h1 style={{ fontSize: '2.25rem', fontWeight: 800, color: 'var(--text-heading)', marginBottom: '0.5rem' }}>PostgreSQL Records</h1>
          <p style={{ color: 'var(--text-muted)' }}>Review and filter previously extracted case sheets stored in the database.</p>
        </div>

        {/* Stats Grid */}
        <div className="db-stats-grid">
          <div className="stat-card">
            <div className="stat-icon">
              <Layers size={24} />
            </div>
            <div className="stat-details">
              <h4>Total Cases Saved</h4>
              <p>{totalCases}</p>
            </div>
          </div>
          <div className="stat-card">
            <div className="stat-icon pending">
              <Calendar size={24} />
            </div>
            <div className="stat-details">
              <h4>Active / Pending</h4>
              <p>{activeCases}</p>
            </div>
          </div>
          <div className="stat-card">
            <div className="stat-icon success">
              <FileText size={24} />
            </div>
            <div className="stat-details">
              <h4>Disposed / Closed</h4>
              <p>{disposedCases}</p>
            </div>
          </div>
        </div>

        {/* Search Control Box */}
        <div className="db-controls">
          <div className="search-box-wrapper">
            <Search className="search-icon-inside" size={18} />
            <input
              type="text"
              placeholder="Search by CNR, Case Status, Type, Petitioner, or Respondent..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              disabled={loading || error}
            />
          </div>
        </div>

        {/* Main Content Area */}
        {loading ? (
          <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', padding: '6rem 0', color: 'var(--text-muted)', gap: '1rem' }}>
            <RefreshCw size={40} className="spin text-primary" />
            <p>Retrieving case structures from PostgreSQL...</p>
          </div>
        ) : error ? (
          <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', padding: '5rem 2rem', background: 'rgba(239, 68, 68, 0.05)', border: '1px solid rgba(239, 68, 68, 0.15)', borderRadius: '1.25rem', color: '#ef4444', gap: '1.25rem', textAlign: 'center' }}>
            <ServerCrash size={48} />
            <h3 style={{ fontSize: '1.25rem', fontWeight: 700 }}>Database Connection Failed</h3>
            <p style={{ maxWidth: '450px', color: 'var(--text-muted)' }}>{error}</p>
            <button onClick={fetchCases} className="btn-primary" style={{ background: '#ef4444' }}>
              Retry Connection
            </button>
          </div>
        ) : filteredCases.length === 0 ? (
          <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', padding: '6rem 2rem', background: 'var(--glass-bg)', border: '1px solid var(--glass-border)', borderRadius: '1.5rem', color: 'var(--text-muted)', gap: '1rem', textAlign: 'center' }}>
            <FileText size={48} className="text-muted" />
            <h3 style={{ fontSize: '1.25rem', fontWeight: 700, color: 'var(--text-heading)' }}>No Records Found</h3>
            <p style={{ maxWidth: '400px' }}>
              {searchTerm ? "No local database entries match your active query. Try searching for a different term." : "Your PostgreSQL cases repository is currently empty. Run a Live Search to extract new case details!"}
            </p>
            {!searchTerm && (
              <Link to="/fetch" className="btn-primary" style={{ marginTop: '0.5rem' }}>
                Go to Scraper
              </Link>
            )}
          </div>
        ) : (
          <div className="db-table-wrapper">
            <div style={{ overflowX: 'auto' }}>
              <table className="modern-table">
                <thead>
                  <tr>
                    <th>CNR Number</th>
                    <th>Petitioners vs. Respondents</th>
                    <th>Case Type</th>
                    <th>Filing Info</th>
                    <th>Status</th>
                    <th>Next Hearing</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredCases.map((c) => {
                    const petitioner = c.petitioners && c.petitioners.length > 0 ? c.petitioners[0] : 'N/A';
                    const respondent = c.respondents && c.respondents.length > 0 ? c.respondents[0] : 'N/A';
                    const isDisposed = (c.caseStatus || '').toLowerCase().includes('disposed') || (c.caseStatus || '').toLowerCase().includes('closed');

                    return (
                      <tr key={c.cnrNumber} onClick={() => setSelectedCase(c)}>
                        <td data-label="CNR Number" style={{ fontWeight: 700, color: 'var(--neon-cyan)' }}>{c.cnrNumber}</td>
                        <td data-label="Parties">
                          <div style={{ fontWeight: 600, color: 'var(--text-heading)' }}>{petitioner}</div>
                          <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', margin: '0.1rem 0' }}>versus</div>
                          <div style={{ color: 'var(--text-main)' }}>{respondent}</div>
                        </td>
                        <td data-label="Case Type">{c.caseType || 'N/A'}</td>
                        <td data-label="Filing Info">
                          <div style={{ fontWeight: 500, color: 'var(--text-heading)' }}>No. {c.filingNumber || 'N/A'}</div>
                          <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>{c.filingDate || 'N/A'}</div>
                        </td>
                        <td data-label="Status">
                          <span className={`status-badge ${isDisposed ? 'muted' : 'success'}`}>
                            {c.caseStatus || 'N/A'}
                          </span>
                        </td>
                        <td data-label="Next Hearing" style={{ color: isDisposed ? 'var(--text-muted)' : 'var(--neon-purple)', fontWeight: 600 }}>
                          {isDisposed ? 'Disposed' : (c.nextHearingDate || 'N/A')}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {/* Sliding Details Drawer Overlay */}
        {selectedCase && (
          <div className="detail-drawer-overlay" onClick={() => setSelectedCase(null)}>
            <div className="detail-drawer" onClick={(e) => e.stopPropagation()}>
              <button className="drawer-close-btn" onClick={() => setSelectedCase(null)}>
                <X size={20} />
              </button>

              <div className="drawer-header">
                <div style={{ fontSize: '0.75rem', color: 'var(--neon-cyan)', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.15em', marginBottom: '0.5rem' }}>
                  Persisted Database Record
                </div>
                <h2 style={{ color: 'var(--text-heading)' }}>CNR: {selectedCase.cnrNumber}</h2>
                <div style={{ display: 'flex', gap: '0.75rem', marginTop: '1.25rem' }}>
                  <button onClick={() => handleExportPdf(selectedCase.cnrNumber)} className="btn-secondary" style={{ padding: '0.5rem 1rem', fontSize: '0.85rem' }} disabled={exportingPdf}>
                    {exportingPdf ? <RefreshCw size={14} className="spin" /> : <Download size={14} />}
                    {exportingPdf ? 'Generating PDF...' : 'Download PDF Report'}
                  </button>
                </div>
              </div>

              {/* Case Properties List */}
              <div style={{ display: 'flex', flexDirection: 'column', gap: '2rem' }}>
                
                {/* Basic Stats Cards */}
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
                  <div style={{ background: 'var(--bg-color)', padding: '1rem', borderRadius: '0.75rem', border: '1px solid var(--border-color)' }}>
                    <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', textTransform: 'uppercase' }}>Filing Info</div>
                    <div style={{ fontSize: '0.95rem', fontWeight: 600, color: 'var(--text-heading)', marginTop: '0.25rem' }}>No: {selectedCase.filingNumber || 'N/A'}</div>
                    <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '0.1rem' }}>Date: {selectedCase.filingDate || 'N/A'}</div>
                  </div>
                  <div style={{ background: 'var(--bg-color)', padding: '1rem', borderRadius: '0.75rem', border: '1px solid var(--border-color)' }}>
                    <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', textTransform: 'uppercase' }}>Registration Info</div>
                    <div style={{ fontSize: '0.95rem', fontWeight: 600, color: 'var(--text-heading)', marginTop: '0.25rem' }}>No: {selectedCase.registrationNumber || 'N/A'}</div>
                    <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '0.1rem' }}>Date: {selectedCase.registrationDate || 'N/A'}</div>
                  </div>
                </div>

                {/* Additional Details */}
                <div style={{ background: 'var(--bg-color)', padding: '1.5rem', borderRadius: '1rem', border: '1px solid var(--border-color)', display: 'flex', flexDirection: 'column', gap: '1rem' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid var(--border-color)', paddingBottom: '0.75rem' }}>
                    <span style={{ color: 'var(--text-muted)' }}>Case Type</span>
                    <span style={{ color: 'var(--text-heading)', fontWeight: 500 }}>{selectedCase.caseType || 'N/A'}</span>
                  </div>
                  <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid var(--border-color)', paddingBottom: '0.75rem' }}>
                    <span style={{ color: 'var(--text-muted)' }}>Court Establishment</span>
                    <span style={{ color: 'var(--text-heading)', fontWeight: 500 }}>{selectedCase.courtEstablishment || 'N/A'}</span>
                  </div>
                  <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                    <span style={{ color: 'var(--text-muted)' }}>Case Status</span>
                    <span style={{ color: 'var(--neon-cyan)', fontWeight: 600 }}>{selectedCase.caseStatus || 'N/A'}</span>
                  </div>
                </div>

                {/* Parties Details */}
                <div>
                  <h3 style={{ fontSize: '1.15rem', color: 'var(--text-heading)', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '1rem' }}>
                    <User size={18} className="text-primary" /> Parties
                  </h3>
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1.5rem' }}>
                    <div>
                      <div style={{ fontWeight: 600, color: 'var(--neon-cyan)', fontSize: '0.85rem', textTransform: 'uppercase' }}>Petitioners</div>
                      <ol style={{ paddingLeft: '1.2rem', marginTop: '0.5rem', color: 'var(--text-main)', fontSize: '0.9rem' }}>
                        {(selectedCase.petitioners || []).map((p, idx) => <li key={idx} style={{ marginBottom: '0.35rem' }}>{p}</li>)}
                      </ol>
                      {selectedCase.petitionerAdvocate && <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '0.5rem' }}>Advocate: {selectedCase.petitionerAdvocate}</div>}
                    </div>
                    <div>
                      <div style={{ fontWeight: 600, color: 'var(--neon-purple)', fontSize: '0.85rem', textTransform: 'uppercase' }}>Respondents</div>
                      <ol style={{ paddingLeft: '1.2rem', marginTop: '0.5rem', color: 'var(--text-main)', fontSize: '0.9rem' }}>
                        {(selectedCase.respondents || []).map((r, idx) => <li key={idx} style={{ marginBottom: '0.35rem' }}>{r}</li>)}
                      </ol>
                      {selectedCase.respondentAdvocate && <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '0.5rem' }}>Advocate: {selectedCase.respondentAdvocate}</div>}
                    </div>
                  </div>
                </div>

                {/* Acts Details */}
                {selectedCase.acts && selectedCase.acts.length > 0 && (
                  <div>
                    <h3 style={{ fontSize: '1.15rem', color: 'var(--text-heading)', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '1rem' }}>
                      <Scale size={18} className="text-primary" /> Acts & Sections
                    </h3>
                    <ul style={{ listStylePosition: 'inside', color: 'var(--text-main)', fontSize: '0.9rem' }}>
                      {selectedCase.acts.map((act, i) => (
                        <li key={i} style={{ marginBottom: '0.35rem' }}>{act}</li>
                      ))}
                    </ul>
                  </div>
                )}

                {/* Historic Hearings */}
                {selectedCase.hearings && selectedCase.hearings.length > 0 && (
                  <div>
                    <h3 style={{ fontSize: '1.15rem', color: 'var(--text-heading)', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '1rem' }}>
                      <Calendar size={18} className="text-primary" /> Hearing History ({selectedCase.hearings.length})
                    </h3>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
                      {selectedCase.hearings.map((h, i) => (
                        <div key={i} style={{ padding: '1rem', background: 'var(--bg-color)', border: '1px solid var(--border-color)', borderRadius: '0.75rem' }}>
                          <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.85rem' }}>
                            <span style={{ fontWeight: 600, color: 'var(--text-heading)' }}>{h.hearingDate}</span>
                            <span style={{ color: 'var(--neon-purple)', fontWeight: 500 }}>{h.purpose}</span>
                          </div>
                          <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '0.25rem' }}>Judge: {h.judge}</div>
                          {h.businessOnDate && <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '0.1rem' }}>Business On: {h.businessOnDate}</div>}
                        </div>
                      ))}
                    </div>
                  </div>
                )}

              </div>
            </div>
          </div>
        )}

      </div>
    </div>
  );
}

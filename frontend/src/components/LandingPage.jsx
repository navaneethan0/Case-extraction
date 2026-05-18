import { Link } from 'react-router-dom';
import { Search, Database, Scale, ArrowRight, ShieldAlert, Cpu } from 'lucide-react';
import './dashboard.css';

export default function LandingPage() {
  return (
    <div className="dashboard-canvas">
      {/* Background ambient glowing circles */}
      <div className="dashboard-header">
        <div className="dashboard-logo">
          <Scale size={40} />
        </div>
        <h1 className="dashboard-title">eCourts Scraper & Explorer</h1>
        <p className="dashboard-subtitle">
          An advanced data extraction platform leveraging native OCR automated CAPTCHA solving and PostgreSQL persistence.
        </p>
      </div>

      <div className="dashboard-grid">
        {/* Fetch Data Card */}
        <Link to="/fetch" className="glass-card">
          <div className="card-icon-box">
            <Search size={36} />
          </div>
          <h2>Live Fetch Data</h2>
          <p>
            Initiate a Playwright-stealth automated browser scraper instance to bypass bot protection and retrieve real-time case profiles.
          </p>
          <div className="card-action-btn">
            Launch Scraper <ArrowRight size={16} />
          </div>
        </Link>

        {/* Database Card */}
        <Link to="/database" className="glass-card">
          <div className="card-icon-box">
            <Database size={36} />
          </div>
          <h2>Database Records</h2>
          <p>
            Explore all persisted case entities indexed from the local PostgreSQL database, query complete historic records, and review hearing profiles.
          </p>
          <div className="card-action-btn">
            Browse Records <ArrowRight size={16} />
          </div>
        </Link>
      </div>

      {/* Tech Specifications / Bottom Bar */}
      <div style={{ marginTop: '6rem', display: 'flex', justifyContent: 'center', gap: '3rem', color: '#64748b', fontSize: '0.85rem', flexWrap: 'wrap', textAlign: 'center', zIndex: 10, position: 'relative' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <Cpu size={16} className="text-primary" />
          <span>ARM64 OCR Automated CAPTCHA</span>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <Database size={16} style={{ color: 'var(--neon-cyan)' }} />
          <span>PostgreSQL JSONB Optimization</span>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <ShieldAlert size={16} style={{ color: 'var(--neon-purple)' }} />
          <span>Stealth Playwright Automation</span>
        </div>
      </div>
    </div>
  );
}

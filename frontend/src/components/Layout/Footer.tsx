import React from 'react';

export const Footer: React.FC = () => {
  return (
    <footer className="footer">
      <div className="footer-container">
        <div className="footer-section">
          <h4>🎬 CineBook</h4>
          <p>Your favorite movie booking platform.</p>
        </div>
        <div className="footer-section">
          <h4>Quick Links</h4>
          <ul>
            <li><a href="/movies">Movies</a></li>
            <li><a href="/login">Login</a></li>
            <li><a href="/register">Sign Up</a></li>
          </ul>
        </div>
        <div className="footer-section">
          <h4>Contact</h4>
          <p>support@cinebook.com</p>
        </div>
        <div className="footer-bottom">
          <p>&copy; {new Date().getFullYear()} CineBook. All rights reserved.</p>
        </div>
      </div>
    </footer>
  );
};

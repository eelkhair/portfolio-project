"use client";

import Link from "next/link";
import { ReactNode, useState } from "react";
import { ThemeToggle } from "./ThemeToggle";

interface NavLink {
  href: string;
  label: string;
  external?: boolean;
}

interface HeaderProps {
  links: NavLink[];
  dropdown?: {
    label: string;
    href: string;
    items: NavLink[];
  };
  dropdownSlot?: ReactNode;
}

export function Header({ links, dropdown, dropdownSlot }: HeaderProps) {
  const [menuOpen, setMenuOpen] = useState(false);

  const closeMenu = () => setMenuOpen(false);

  return (
    <header>
      <nav aria-label="Main navigation">
        <div className="nav-inner">
          <Link href="/" className="logo" aria-label="Home">EE</Link>
          <ul className={`nav-links${menuOpen ? " open" : ""}`}>
            {links.map((link) => (
              <li key={link.href}>
                {link.external ? (
                  <a href={link.href} target="_blank" rel="noopener noreferrer" onClick={closeMenu}>{link.label}</a>
                ) : link.href.startsWith("#") ? (
                  <a href={link.href} onClick={closeMenu}>{link.label}</a>
                ) : (
                  <Link href={link.href} onClick={closeMenu}>{link.label}</Link>
                )}
              </li>
            ))}
            {dropdown && (
              <li className="nav-dropdown">
                <a href={dropdown.href} aria-haspopup="true">{dropdown.label}</a>
                <ul className="nav-dropdown-menu">
                  {dropdown.items.map((item) => (
                    <li key={item.href}>
                      <Link href={item.href} onClick={closeMenu}>{item.label}</Link>
                    </li>
                  ))}
                </ul>
              </li>
            )}
            {dropdownSlot}
          </ul>
          <div className="nav-right">
            <ThemeToggle />
            <button
              className={`hamburger${menuOpen ? " active" : ""}`}
              aria-label={menuOpen ? "Close menu" : "Open menu"}
              aria-expanded={menuOpen}
              onClick={() => setMenuOpen(!menuOpen)}
            >
              <span /><span /><span />
            </button>
          </div>
        </div>
      </nav>
    </header>
  );
}

import * as React from 'react';
import MuiTabs from '@mui/material/Tabs';
import MuiTab from '@mui/material/Tab';
import Box from '@mui/material/Box';

interface TabsProps {
  value: string;
  onValueChange: (v: string) => void;
  children: React.ReactNode;
  className?: string;
}

function Tabs({ value, onValueChange, children, className }: TabsProps) {
  return (
    <Box className={className}>
      {React.Children.map(children, child => {
        if (React.isValidElement(child)) {
          return React.cloneElement(
            child as React.ReactElement<{ value?: string; onValueChange?: (v: string) => void }>,
            { value, onValueChange }
          );
        }
        return child;
      })}
    </Box>
  );
}

interface TabsListProps extends React.HTMLAttributes<HTMLDivElement> {
  value?: string;
  onValueChange?: (v: string) => void;
}

function TabsList({ children, value, onValueChange, className }: TabsListProps) {
  const handleChange = (_: React.SyntheticEvent, v: string) => onValueChange?.(v);
  return (
    <MuiTabs value={value} onChange={handleChange} className={className} variant="scrollable" scrollButtons="auto">
      {children}
    </MuiTabs>
  );
}

interface TabsTriggerProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  value: string;
}

function TabsTrigger({ value, children, ...rest }: TabsTriggerProps) {
  return <MuiTab label={children} value={value} {...(rest as object)} />;
}

interface TabsContentProps extends React.HTMLAttributes<HTMLDivElement> {
  value: string;
  activeValue?: string;
}

function TabsContent({ value, activeValue, children, ...rest }: TabsContentProps) {
  if (value !== activeValue) return null;
  return <Box sx={{ pt: 2 }} {...(rest as object)}>{children}</Box>;
}

export { Tabs, TabsList, TabsTrigger, TabsContent };

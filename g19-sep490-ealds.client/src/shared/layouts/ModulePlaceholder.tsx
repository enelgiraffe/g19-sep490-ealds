interface ModulePlaceholderProps {
  title: string;
}

export function ModulePlaceholder({ title }: ModulePlaceholderProps) {
  return (
    <div>
      <h1>{title}</h1>
      <p>Module placeholder. Content will be implemented later.</p>
    </div>
  );
}

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// C# 9.0 の init; や record 型を、古い .NET バージョンで利用するために必要な型。
    /// このクラスをプロジェクトに追加することで、CS0518 エラーを回避できます。
    /// </summary>
    internal static class IsExternalInit
    {
    }
}
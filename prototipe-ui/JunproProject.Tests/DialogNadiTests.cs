using System.Windows.Forms;
using Xunit;

namespace Junpro_Project.Tests;

// Temuan Dharu (M-02): dialog Hapus Riwayat memakai tombol Yes/No berbahasa Inggris.
// DialogNadi harus berbahasa Indonesia dan aman: hanya "Ya" yang berarti setuju menghapus.
public class DialogNadiTests
{
    private static List<Button> Tombol(Control c) =>
        c.Controls.Cast<Control>().SelectMany(x => (x is Button b ? new[] { b } : Array.Empty<Button>()).Concat(Tombol(x))).ToList();

    [Fact]
    public void Konfirmasi_TombolBerbahasaIndonesia()
    {
        using Form d = DialogNadi.BuatKonfirmasi("Hapus seluruh riwayat chat untuk Sleman?", "Hapus Riwayat");

        Assert.Equal(new[] { "Tidak", "Ya" }, Tombol(d).Select(b => b.Text).OrderBy(t => t, StringComparer.Ordinal));
        Assert.DoesNotContain(Tombol(d), b => b.Text is "Yes" or "No" or "OK" or "Cancel");
    }

    [Fact]
    public void Konfirmasi_HanyaYaYangBerartiSetuju()
    {
        using Form d = DialogNadi.BuatKonfirmasi("Hapus?", "Hapus Riwayat");
        Button ya = Tombol(d).Single(b => b.Text == "Ya");
        Button tidak = Tombol(d).Single(b => b.Text == "Tidak");

        Assert.Equal(DialogResult.Yes, ya.DialogResult);
        Assert.NotEqual(DialogResult.Yes, tidak.DialogResult);
        Assert.Same(tidak, d.AcceptButton);   // Enter = Tidak
        Assert.Same(tidak, d.CancelButton);   // Esc = Tidak
    }
}

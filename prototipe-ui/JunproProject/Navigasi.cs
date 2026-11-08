namespace Junpro_Project;

// Mengatur perpindahan antar-form. Setiap form dibuat sekali lalu dipakai
// ulang, sehingga isian (mis. riwayat chat, indikator terpilih) tidak hilang
// saat pengguna bolak-balik halaman.
public static class Navigasi
{
    private static readonly Dictionary<Type, FormDasar> daftarForm = new();

    public static T Ambil<T>() where T : FormDasar => (T)Ambil(typeof(T));

    public static void Buka<T>(Form asal) where T : FormDasar => Buka(typeof(T), asal);

    public static void Buka(Type tujuan, Form asal)
    {
        FormDasar form = Ambil(tujuan);
        if (form == asal) return;

        // Form tujuan menempati posisi & ukuran yang sama dengan form asal,
        // supaya perpindahan terasa seperti ganti halaman, bukan jendela baru.
        form.StartPosition = FormStartPosition.Manual;
        if (asal.WindowState == FormWindowState.Maximized)
        {
            form.WindowState = FormWindowState.Normal;
            form.Bounds = asal.RestoreBounds;
            form.WindowState = FormWindowState.Maximized;
        }
        else
        {
            form.WindowState = FormWindowState.Normal;
            form.Bounds = asal.Bounds;
        }

        form.Show();
        form.Activate();
        asal.Hide();
    }

    private static FormDasar Ambil(Type tipe)
    {
        if (!daftarForm.TryGetValue(tipe, out FormDasar? form) || form.IsDisposed)
        {
            form = (FormDasar)Activator.CreateInstance(tipe)!;
            daftarForm[tipe] = form;
        }
        return form;
    }
}

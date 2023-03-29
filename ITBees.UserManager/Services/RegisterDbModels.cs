﻿using ITBees.Models.Companies;
using ITBees.Models.EmailAccounts;
using ITBees.Models.Languages;
using ITBees.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace ITBees.UserManager.Services
{
    public class DbModelBuilder
    {
        public static void Register(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserAccount>().HasKey(x => x.Guid);
            modelBuilder.Entity<EmailAccount>();
            modelBuilder.Entity<Company>().HasKey(x => x.Guid);
            modelBuilder.Entity<UsersInCompany>().HasKey(x => x.Guid);
            modelBuilder.Entity<Language>().HasKey(x => x.Id);
            modelBuilder.Entity<Language>().HasDiscriminator<string>("LanguageType")
                .HasValue<Aa>(nameof(Aa) + "Type")
                .HasValue<Ab>(nameof(Ab) + "Type")
                .HasValue<Ae>(nameof(Ae) + "Type")
                .HasValue<Af>(nameof(Af) + "Type")
                .HasValue<Ak>(nameof(Ak) + "Type")
                .HasValue<Am>(nameof(Am) + "Type")
                .HasValue<An>(nameof(An) + "Type")
                .HasValue<Ar>(nameof(Ar) + "Type")
                .HasValue<As>(nameof(As) + "Type")
                .HasValue<Av>(nameof(Av) + "Type")
                .HasValue<Ay>(nameof(Ay) + "Type")
                .HasValue<Az>(nameof(Az) + "Type")
                .HasValue<Ba>(nameof(Ba) + "Type")
                .HasValue<Be>(nameof(Be) + "Type")
                .HasValue<Bg>(nameof(Bg) + "Type")
                .HasValue<Bh>(nameof(Bh) + "Type")
                .HasValue<Bi>(nameof(Bi) + "Type")
                .HasValue<Bm>(nameof(Bm) + "Type")
                .HasValue<Bn>(nameof(Bn) + "Type")
                .HasValue<Bo>(nameof(Bo) + "Type")
                .HasValue<Br>(nameof(Br) + "Type")
                .HasValue<Bs>(nameof(Bs) + "Type")
                .HasValue<Ca>(nameof(Ca) + "Type")
                .HasValue<Ce>(nameof(Ce) + "Type")
                .HasValue<Ch>(nameof(Ch) + "Type")
                .HasValue<Co>(nameof(Co) + "Type")
                .HasValue<Cr>(nameof(Cr) + "Type")
                .HasValue<Cs>(nameof(Cs) + "Type")
                .HasValue<Cu>(nameof(Cu) + "Type")
                .HasValue<Cv>(nameof(Cv) + "Type")
                .HasValue<Cy>(nameof(Cy) + "Type")
                .HasValue<Da>(nameof(Da) + "Type")
                .HasValue<De>(nameof(De) + "Type")
                .HasValue<Dv>(nameof(Dv) + "Type")
                .HasValue<Dz>(nameof(Dz) + "Type")
                .HasValue<Ee>(nameof(Ee) + "Type")
                .HasValue<El>(nameof(El) + "Type")
                .HasValue<En>(nameof(En) + "Type")
                .HasValue<Eo>(nameof(Eo) + "Type")
                .HasValue<Es>(nameof(Es) + "Type")
                .HasValue<Et>(nameof(Et) + "Type")
                .HasValue<Eu>(nameof(Eu) + "Type")
                .HasValue<Fa>(nameof(Fa) + "Type")
                .HasValue<Ff>(nameof(Ff) + "Type")
                .HasValue<Fi>(nameof(Fi) + "Type")
                .HasValue<Fj>(nameof(Fj) + "Type")
                .HasValue<Fo>(nameof(Fo) + "Type")
                .HasValue<Fr>(nameof(Fr) + "Type")
                .HasValue<Fy>(nameof(Fy) + "Type")
                .HasValue<Ga>(nameof(Ga) + "Type")
                .HasValue<Gd>(nameof(Gd) + "Type")
                .HasValue<Gl>(nameof(Gl) + "Type")
                .HasValue<Gn>(nameof(Gn) + "Type")
                .HasValue<Gu>(nameof(Gu) + "Type")
                .HasValue<Gv>(nameof(Gv) + "Type")
                .HasValue<Ha>(nameof(Ha) + "Type")
                .HasValue<He>(nameof(He) + "Type")
                .HasValue<Hi>(nameof(Hi) + "Type")
                .HasValue<Ho>(nameof(Ho) + "Type")
                .HasValue<Hr>(nameof(Hr) + "Type")
                .HasValue<Ht>(nameof(Ht) + "Type")
                .HasValue<Hu>(nameof(Hu) + "Type")
                .HasValue<Hy>(nameof(Hy) + "Type")
                .HasValue<Hz>(nameof(Hz) + "Type")
                .HasValue<Ia>(nameof(Ia) + "Type")
                .HasValue<Id>(nameof(Id) + "Type")
                .HasValue<Ie>(nameof(Ie) + "Type")
                .HasValue<Ig>(nameof(Ig) + "Type")
                .HasValue<Ii>(nameof(Ii) + "Type")
                .HasValue<Ik>(nameof(Ik) + "Type")
                .HasValue<Io>(nameof(Io) + "Type")
                .HasValue<Is>(nameof(Is) + "Type")
                .HasValue<It>(nameof(It) + "Type")
                .HasValue<Iu>(nameof(Iu) + "Type")
                .HasValue<Ja>(nameof(Ja) + "Type")
                .HasValue<Jv>(nameof(Jv) + "Type")
                .HasValue<Ka>(nameof(Ka) + "Type")
                .HasValue<Kg>(nameof(Kg) + "Type")
                .HasValue<Ki>(nameof(Ki) + "Type")
                .HasValue<Kj>(nameof(Kj) + "Type")
                .HasValue<Kk>(nameof(Kk) + "Type")
                .HasValue<Kl>(nameof(Kl) + "Type")
                .HasValue<Km>(nameof(Km) + "Type")
                .HasValue<Kn>(nameof(Kn) + "Type")
                .HasValue<Ko>(nameof(Ko) + "Type")
                .HasValue<Kr>(nameof(Kr) + "Type")
                .HasValue<Ks>(nameof(Ks) + "Type")
                .HasValue<Ku>(nameof(Ku) + "Type")
                .HasValue<Kv>(nameof(Kv) + "Type")
                .HasValue<Kw>(nameof(Kw) + "Type")
                .HasValue<Ky>(nameof(Ky) + "Type")
                .HasValue<La>(nameof(La) + "Type")
                .HasValue<Lb>(nameof(Lb) + "Type")
                .HasValue<Lg>(nameof(Lg) + "Type")
                .HasValue<Li>(nameof(Li) + "Type")
                .HasValue<Ln>(nameof(Ln) + "Type")
                .HasValue<Lo>(nameof(Lo) + "Type")
                .HasValue<Lt>(nameof(Lt) + "Type")
                .HasValue<Lu>(nameof(Lu) + "Type")
                .HasValue<Lv>(nameof(Lv) + "Type")
                .HasValue<Mg>(nameof(Mg) + "Type")
                .HasValue<Mh>(nameof(Mh) + "Type")
                .HasValue<Mi>(nameof(Mi) + "Type")
                .HasValue<Mk>(nameof(Mk) + "Type")
                .HasValue<Ml>(nameof(Ml) + "Type")
                .HasValue<Mn>(nameof(Mn) + "Type")
                .HasValue<Mr>(nameof(Mr) + "Type")
                .HasValue<Ms>(nameof(Ms) + "Type")
                .HasValue<Mt>(nameof(Mt) + "Type")
                .HasValue<My>(nameof(My) + "Type")
                .HasValue<Na>(nameof(Na) + "Type")
                .HasValue<Nb>(nameof(Nb) + "Type")
                .HasValue<Nd>(nameof(Nd) + "Type")
                .HasValue<Ne>(nameof(Ne) + "Type")
                .HasValue<Ng>(nameof(Ng) + "Type")
                .HasValue<Nl>(nameof(Nl) + "Type")
                .HasValue<Nn>(nameof(Nn) + "Type")
                .HasValue<No>(nameof(No) + "Type")
                .HasValue<Nr>(nameof(Nr) + "Type")
                .HasValue<Nv>(nameof(Nv) + "Type")
                .HasValue<Ny>(nameof(Ny) + "Type")
                .HasValue<Oc>(nameof(Oc) + "Type")
                .HasValue<Oj>(nameof(Oj) + "Type")
                .HasValue<Om>(nameof(Om) + "Type")
                .HasValue<Or>(nameof(Or) + "Type")
                .HasValue<Os>(nameof(Os) + "Type")
                .HasValue<Pa>(nameof(Pa) + "Type")
                .HasValue<Pi>(nameof(Pi) + "Type")
                .HasValue<Pl>(nameof(Pl) + "Type")
                .HasValue<Ps>(nameof(Ps) + "Type")
                .HasValue<Pt>(nameof(Pt) + "Type")
                .HasValue<Qu>(nameof(Qu) + "Type")
                .HasValue<Rm>(nameof(Rm) + "Type")
                .HasValue<Rn>(nameof(Rn) + "Type")
                .HasValue<Ro>(nameof(Ro) + "Type")
                .HasValue<Ru>(nameof(Ru) + "Type")
                .HasValue<Rw>(nameof(Rw) + "Type")
                .HasValue<Sa>(nameof(Sa) + "Type")
                .HasValue<Sc>(nameof(Sc) + "Type")
                .HasValue<Sd>(nameof(Sd) + "Type")
                .HasValue<Se>(nameof(Se) + "Type")
                .HasValue<Sg>(nameof(Sg) + "Type")
                .HasValue<Si>(nameof(Si) + "Type")
                .HasValue<Sk>(nameof(Sk) + "Type")
                .HasValue<Sl>(nameof(Sl) + "Type")
                .HasValue<Sm>(nameof(Sm) + "Type")
                .HasValue<Sn>(nameof(Sn) + "Type")
                .HasValue<So>(nameof(So) + "Type")
                .HasValue<Sq>(nameof(Sq) + "Type")
                .HasValue<Sr>(nameof(Sr) + "Type")
                .HasValue<Ss>(nameof(Ss) + "Type")
                .HasValue<St>(nameof(St) + "Type")
                .HasValue<Su>(nameof(Su) + "Type")
                .HasValue<Sv>(nameof(Sv) + "Type")
                .HasValue<Sw>(nameof(Sw) + "Type")
                .HasValue<Ta>(nameof(Ta) + "Type")
                .HasValue<Te>(nameof(Te) + "Type")
                .HasValue<Tg>(nameof(Tg) + "Type")
                .HasValue<Th>(nameof(Th) + "Type")
                .HasValue<Ti>(nameof(Ti) + "Type")
                .HasValue<Tk>(nameof(Tk) + "Type")
                .HasValue<Tl>(nameof(Tl) + "Type")
                .HasValue<Tn>(nameof(Tn) + "Type")
                .HasValue<To>(nameof(To) + "Type")
                .HasValue<Tr>(nameof(Tr) + "Type")
                .HasValue<Ts>(nameof(Ts) + "Type")
                .HasValue<Tt>(nameof(Tt) + "Type")
                .HasValue<Tw>(nameof(Tw) + "Type")
                .HasValue<Ty>(nameof(Ty) + "Type")
                .HasValue<Ug>(nameof(Ug) + "Type")
                .HasValue<Uk>(nameof(Uk) + "Type")
                .HasValue<Ur>(nameof(Ur) + "Type")
                .HasValue<Uz>(nameof(Uz) + "Type")
                .HasValue<Ve>(nameof(Ve) + "Type")
                .HasValue<Vi>(nameof(Vi) + "Type")
                .HasValue<Vo>(nameof(Vo) + "Type")
                .HasValue<Wa>(nameof(Wa) + "Type")
                .HasValue<Wo>(nameof(Wo) + "Type")
                .HasValue<Xh>(nameof(Xh) + "Type")
                .HasValue<Yi>(nameof(Yi) + "Type")
                .HasValue<Yo>(nameof(Yo) + "Type")
                .HasValue<Za>(nameof(Za) + "Type")
                .HasValue<Zh>(nameof(Zh) + "Type")
                .HasValue<Zu>(nameof(Zu) + "Type");
        }
    }
}
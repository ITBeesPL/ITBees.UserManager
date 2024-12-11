using Microsoft.EntityFrameworkCore.Migrations;

namespace ITBees.UserManager.DbModels;

public class MigrationScriptRelatedToFasRoleIdentity
{
    public static void ApplyRemoveIndex(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_AspNetUserTokens_AspNetUsers_UserId",
            table: "AspNetUserTokens"); 
        migrationBuilder.DropForeignKey(
            name: "FK_AspNetUserLogins_AspNetUsers_UserId",
            table: "AspNetUserLogins");
        migrationBuilder.DropForeignKey(
            name: "FK_AspNetUserRoles_AspNetUsers_UserId",
            table: "AspNetUserRoles"); 
        migrationBuilder.DropForeignKey(
            name: "FK_AspNetUserClaims_AspNetUsers_UserId",
            table: "AspNetUserClaims");
        migrationBuilder.DropForeignKey(
            name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
            table: "AspNetUserRoles");
        migrationBuilder.DropForeignKey(
            name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
            table: "AspNetRoleClaims");
    }

    public static void ApplyAddIndex(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddForeignKey(
            name: "FK_AspNetUserTokens_AspNetUsers_UserId",
            table: "AspNetUserTokens",
            column: "UserId",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade); 
        
        migrationBuilder.AddForeignKey(
            name: "FK_AspNetUserLogins_AspNetUsers_UserId",
            table: "AspNetUserLogins",
            column: "UserId",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade); 
        
        migrationBuilder.AddForeignKey(
            name: "FK_AspNetUserRoles_AspNetUsers_UserId",
            table: "AspNetUserRoles",
            column: "UserId",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);  
        
        migrationBuilder.AddForeignKey(
            name: "FK_AspNetUserClaims_AspNetUsers_UserId",
            table: "AspNetUserClaims",
            column: "UserId",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
        
        migrationBuilder.AddForeignKey(
            name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
            table: "AspNetUserClaims",
            column: "UserId",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);  
        
        migrationBuilder.AddForeignKey(
            name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
            table: "AspNetRoleClaims",
            column: "RoleId",
            principalTable: "AspNetRoles",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
export interface ProductionEfficiencyDTO {
    category: string;
    detail: ProductionEfficiencyDetail[];
}
export interface ProductionEfficiencyDetail {
    shift: string;
    mdat: string;
    nameEN: string;
    rmodel: string;
    qty: number;
}
export interface ProductionEfficiencyParam {
  productionDate: string;
  class: string;
  lang?: string;
}
